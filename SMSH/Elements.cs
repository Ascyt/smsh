using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Program;
using System.Xml.Linq;

namespace Elements
{
    public partial class Elements
    {
        public const string KEYWORD_CHARS = ".,>#:$";

        public List<Section> sections = new();
        public string? title;
        public bool isLightTheme = false;
        public string toTopText = "Back to top";
        public bool credit = true;
        public Dictionary<string, string> customClasses = new();
        public static int spaces = 0; // Amount of spaces used for indentation. 0 = tab

        private readonly static Dictionary<string, char[]> getAttributes = new Dictionary<string, char[]>()
            {
                {"class", new char[2] { '(', ')' }},
                {"style", new char[2] { '[', ']' }},
                {"href", new char[2] { '{', '}' }},
            };


        public Elements(string markup)
        {
            Section? currentSection = null;

            // Go through each line
            string[] lines = markup.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Length == 0)
                    continue;

                switch (line[0])
                {
                    case '#': // Section
                        if (currentSection != null)
                            sections.Add(currentSection);

                        currentSection = new Section(line.Substring(1).Trim(), true);
                        break;
                    case '\t':
                        if (spaces != 0)
                        {
                            throw new CodeException($"Invalid indentation (expected {spaces} spaces, not tabs)", i, 0);
                        }
                        if (line.Trim() != "")
                            throw new CodeException("Too large amount of indentation (expected none).", i, 0);
                        break;
                    case ' ':
                        if (spaces == 0)
                        {
                            throw new CodeException("Invalid indentation (expected tab, not space).", i, 0);
                        }
                        break;
                    default:
                        TryAddElement(GetElement(line, ref i, 0), currentSection?.elements);
                        break;
                }
            }

            if (currentSection != null)
                sections.Add(currentSection);

            void TryAddElement(Element? element, List<Element>? elementList)
            {
                if (element != null && elementList != null)
                {
                    elementList.Add(element);
                }
            }


            Element? GetElement(string line, ref int i, int indents)
            {
                string trimmedLine = line.Substring(GetIndentIndex(indents));

                if (trimmedLine.TrimStart().Length == 0 || trimmedLine[0] == '>')
                    return null;

                if ("\t ".Contains(trimmedLine[0]))
                    throw new CodeException($"Invalid indentation (expected {indents}).", i, indents);

                if (!KEYWORD_CHARS.Contains(trimmedLine[0]))
                {
                    return GetElement(line.Insert(GetIndentIndex(indents), ". "), ref i, indents);
                }


                Dictionary<string, string> attributes = ExtractAttributes(i, ref trimmedLine, getAttributes);

                string tag = trimmedLine.Split(' ')[0];
                trimmedLine = FormatText(trimmedLine, indents, i);

                if (tag == ".")
                {
                    trimmedLine = trimmedLine[trimmedLine.Length - 1] == '\\' ?
                            trimmedLine.Substring(0, trimmedLine.Length - 1) :
                            trimmedLine + "<br>";
                }

                if (tag == ".img")
                {
                    string alt = trimmedLine.Substring(tag.Length).Trim();
                    if (alt == "")
                        alt = "Image";

                    attributes["alt"] = alt;

                    Element imageElement = new Element(tag, "", attributes);

                    if (!imageElement.attributes.ContainsKey("href"))
                        throw new CodeException("Image must have a link attribute.", i);

                    imageElement.attributes["src"] = imageElement.attributes["href"];
                    imageElement.attributes.Remove("href");

                    return imageElement;
                }

                string? customClass = null;

                switch (trimmedLine[0])
                {
                    case ',': // Margin
                        const int DEFAULT_MARGIN = 16;

                        string amount = trimmedLine.Substring(1).Trim();
                        if (amount == "")
                            amount = $"{DEFAULT_MARGIN}px";

                        int amountInt;
                        if (int.TryParse(amount, out amountInt))
                        {
                            amount = $"{amountInt * DEFAULT_MARGIN}px";
                        }

                        return new Element(".div", "", new Dictionary<string, string>()
                        {
                            {"style", $"margin-bottom:{amount};"},
                        });
                    case '#': // Section
                        throw new CodeException("Section can't be defined in elements.", i, indents);
                    case ':': // Special tag
                        if (currentSection != null)
                            throw new CodeException("Special tag must be defined above all sections.", i);

                        switch (line.Split(' ')[0].Substring(1).ToLower())
                        {
                            case "title":
                                if (title != null)
                                    throw new CodeException("Title already defined.", i);

                                title = line.Substring(7).Trim();
                                return null;
                            case "theme":
                                string[] light = {"light", "l"};
                                string[] dark = {"dark", "d"};

                                string theme = line.Substring(6).Trim().ToLower();
                                bool isLight = light.Contains(theme);
                                bool isDark = dark.Contains(theme);

                                if (!isLight && !isDark)
                                    throw new CodeException("Invalid theme. (Valid options: light, dark)", i, line.Length - theme.Length);

                                isLightTheme = isLight;
                                return null;
                            case "totoptext":
                                toTopText = line.Substring(11).Trim();
                                return null;
                            case "hidecredit":
                                credit = false;
                                return null;
                            case "spaces":
                                spaces = int.Parse(line.Substring(7).Trim());
                                return null;
                            default:
                                throw new CodeException("Invalid special tag.", i);
                        }
                    case '$': // Custom class
                        if (currentSection != null)
                            throw new CodeException("Custom CSS class must be defined above all sections.", i);

                        customClass = trimmedLine.Substring(1).Trim();

                        if (customClasses.ContainsKey(trimmedLine.Substring(1).Trim()))
                            throw new CodeException($"Custom CSS class \"{customClass}\" already defined.", i);

                        customClasses[customClass] = "";

                        break;
                    default:
                        if (currentSection == null)
                            throw new CodeException("No section defined.", i);
                        break;
                }

                Element element = new Element(tag, trimmedLine.Length == tag.Length + 1 ? "" : trimmedLine.Substring(tag.Length + 2), attributes);

                i++;

                if (i >= lines.Length)
                    return element;

                int actualIndents = ActualIndents(lines[i]);

                while (actualIndents > indents)
                {
                    if (lines[i].Trim().Length > 0)
                    {
                        if (customClass != null)
                        {
                            customClasses[customClass] += lines[i].Trim();
                            break;
                        }

                        TryAddElement(GetElement(lines[i], ref i, actualIndents), element.elements);
                    }

                    i++;

                    if (i >= lines.Length)
                        return element;

                    actualIndents = ActualIndents(lines[i]);
                }
                i--;

                return element;

                int ActualIndents(string text)
                {
                    if (spaces == 0)
                    {
                        for (int i = 0; i < text.Length; i++)
                        {
                            if (text[i] != '\t')
                                return i;
                        }
                    }
                    else
                    {
                        int i = 0;
                        while (i < text.Length && text[i] == ' ')
                            i += spaces;
                        return i / spaces;
                    }
                    return text.Length;
                }
            }
        }

        public static int GetIndentIndex(int indents)
            => spaces == 0 ? indents : indents * spaces;
    }
}

