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
        public const string KEYWORD_CHARS = ".,>#:";

        public List<Section> sections = new List<Section>();
        public string? title;
        public bool isLightTheme = false;
        public string toTopText = "Back to top";
        public bool credit = true;

        public readonly static Dictionary<string, char[]> getAttributes = new Dictionary<string, char[]>()
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
                        if (line.Trim() != "")
                            throw new CodeException("Too large amount of indentation (expected none).", i, 0);
                        break;
                    case ' ':
                        throw new CodeException("Invalid indentation (expected tab, not space).", i, 0);
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
                string trimmedLine = line.TrimStart();

                if (trimmedLine.Length == 0 || trimmedLine[0] == '>')
                    return null;

                if (!KEYWORD_CHARS.Contains(trimmedLine[0]))
                {
                    return GetElement(line.Insert(indents, ". "), ref i, indents);
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
                            default:
                                throw new CodeException("Invalid special tag.", i);
                        }
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
                        string newElementTag = lines[i].Substring(actualIndents).Split(' ')[0];

                        switch (newElementTag[0])
                        {
                            case '>': // Comment
                                break;
                            case '\\':
                            default: // Tag, Text, Margin
                                TryAddElement(GetElement(lines[i], ref i, indents + 1), element.elements);
                                break;
                        }
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
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (text[i] != '\t')
                            return i;
                    }
                    return text.Length;
                }
            }
        }
    }
}

