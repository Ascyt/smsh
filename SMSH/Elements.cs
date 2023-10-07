using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Program;
using System.Xml.Linq;

namespace Elements
{
    public class Elements
    {
        public const string KEYWORD_CHARS = ".,>#:";

        public List<Section> sections = new List<Section>();
        public string? title;
        public bool isLightTheme = false;
        public string toTopText = "Back to top";

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
                    return GetElement(". " + line, ref i, indents);
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
                        /*if (indents > 0)
                            throw new CodeException("Special tags can't be defined in elements.", i, indents);*/

                        if (currentSection != null)
                            throw new CodeException("Special tag must be defined above all sections.", i);

                        switch (line.Split(' ')[0])
                        {
                            case ":title":
                                if (title != null)
                                    throw new CodeException("Title already defined.", i);

                                title = line.Substring(7).Trim();
                                return null;
                            case ":theme":
                                string[] light = {"light", "l"};
                                string[] dark = {"dark", "d"};

                                string theme = line.Substring(6).Trim().ToLower();
                                bool isLight = light.Contains(theme);
                                bool isDark = dark.Contains(theme);

                                if (!isLight && !isDark)
                                    throw new CodeException("Invalid theme. (Valid options: light, dark)", i, line.Length - theme.Length);

                                isLightTheme = isLight;
                                return null;
                            case ":totoptext":
                            case ":toTopText":
                                toTopText = line.Substring(11).Trim();
                                return null;
                            default:
                                throw new CodeException("Invalid special tag.", i);
                        }
                    default:
                        if (currentSection == null)
                            throw new CodeException("No section defined.", i);
                        break;
                }

                Element element = new Element(tag, trimmedLine.Length == tag.Length ? "" : trimmedLine.Substring(tag.Length + 1), attributes);

                i++;

                if (i >= lines.Length)
                    return element;

                int actualIndents = ActualIndents(lines[i]);

                while (actualIndents > indents)
                {
                    if (lines[i].Trim().Length > 0)
                    {
                        string newElementTag = lines[i].TrimStart().Split(' ')[0];

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

        static Dictionary<string, string> ExtractAttributes(int lineIndex, ref string trimmedLine, Dictionary<string, char[]> attributes)
        {
            string newTrimmedLine = "";
            Dictionary<string, string> result = new Dictionary<string, string>();

            string? inAttribute = null;
            bool hasChangedAttribute = false;

            int i;
            for (i = 0; i < trimmedLine.Length; i++)
            {
                if (TryChangeAttribute(trimmedLine[i]))
                {
                    hasChangedAttribute = true;

                    continue;
                }

                if (inAttribute != null)
                {
                    if (!result.ContainsKey(inAttribute))
                        result[inAttribute] = "";
                    result[inAttribute] += trimmedLine[i];

                    continue;
                }

                if (trimmedLine[i] == ' ')
                {
                    break;
                }

                if (hasChangedAttribute)
                {
                    throw new CodeException($"Unexpected character outside of attribute: '{trimmedLine[i]}'.", lineIndex, i);
                }
                newTrimmedLine += trimmedLine[i];
            }

            trimmedLine = newTrimmedLine + ' ' + trimmedLine.Substring(i);
            return result;

            bool TryChangeAttribute(char c)
            {
                foreach (KeyValuePair<string, char[]> attribute in attributes)
                {
                    if (inAttribute == null)
                    {
                        if (c == attribute.Value[0])
                        {
                            inAttribute = attribute.Key;
                            return true;
                        }
                        continue;
                    }

                    if (c == attribute.Value[1])
                    {
                        if (inAttribute == attribute.Key)
                        {
                            inAttribute = null;
                            return true;
                        }

                        // TODO: Add add line and column=index number to exception
                        throw new CodeException($"Unexpected attribute closing character: '{c}'.", -1);
                    }
                }
                return false;
            }
        }

        
        public static string FormatText(string text, int indents, int lineIndex)
        {
            text = text.Replace("\\<", "&lt;").Replace("\\>", "&gt;");

            string result = "";
            string currentFormat = "";
            int formattingDelta = 0;
            const char FORMAT_START = '<';
            const char FORMAT_END = '>';

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == FORMAT_START)
                {
                    formattingDelta++;

                    if (formattingDelta == 1)
                        continue;
                }

                if (text[i] == FORMAT_END)
                {
                    formattingDelta--;

                    if (formattingDelta < 0)
                        throw new CodeException($"Unexpected format closing tag.", lineIndex, indents, i);

                    if (formattingDelta == 0)
                    {
                        if (new string[] { "br", "hr", "img", "input", "meta", "link" }.Contains(currentFormat.Trim()))
                        {
                            result += $"<{currentFormat}>";
                        }
                        else
                        {
                            Dictionary<string,string> attributes = ExtractAttributes(lineIndex, ref currentFormat, getAttributes);

                            string formatTag = currentFormat.Split(' ')[0];

                            currentFormat = FormatText(currentFormat.Substring(formatTag.Length).TrimStart(), indents, lineIndex);

                            Element element = new Element(formatTag, currentFormat, attributes);
                            result += element.ToString();
                        }

                        currentFormat = "";

                        continue;
                    }
                }

                if (formattingDelta > 0)
                {
                    currentFormat += text[i];
                    continue;
                }

                result += text[i];
            }

            if (formattingDelta > 0)
                throw new CodeException("Unclosed format tag.", lineIndex, indents, text.Length);

            return result;
        }

        public class Section
        {
            public string name;
            public List<Element> elements = new List<Element>();

            public bool withTitle;

            public Section(string name, bool withTitle)
            {
                this.name = name;
                this.withTitle = withTitle;
            }

            public string FormattedName => name.Replace(" ", "-").Replace("<", "&lt;").Replace(">", "&gt;").ToLower();

            public override string ToString()
            {
                string result = $"<section id=\"{FormattedName}\">";

                if (withTitle)
                    result += $"<h1>{name}</h1>";

                foreach (Element element in elements)
                    result += element.ToString();

                return result + "</section>";
            }
        }

        public class Element
        {
            public string tag;
            public readonly Dictionary<string, string> attributes = new Dictionary<string, string>();
            
            public string text;
            public List<Element> elements = new List<Element>();

            public Element(string tag, string text = "", Dictionary<string, string>? attributes = null)
            {
                if (tag.Length > 0 && (tag[0] == '.' || tag[0] == ':'))
                    tag = tag.Substring(1);

                this.attributes = attributes ?? new Dictionary<string, string>();

                if (new string[] { "card" }.Contains(tag))
                {
                    if (this.attributes.ContainsKey("class"))
                        this.attributes["class"] += " " + tag;
                    else
                        this.attributes["class"] = tag;
                    this.tag = "div";
                }
                else
                {
                    if (tag == "")
                    {
                        if (this.attributes.ContainsKey("class"))
                            this.attributes["class"] = "t " + this.attributes["class"];
                        else
                            this.attributes["class"] = "t";

                        this.tag = "span";
                    }
                    else
                        this.tag = tag;
                }

                this.text = text;
            }
           
            public override string ToString()
            {
                string result = $"<{tag}";

                // attributes
                foreach (KeyValuePair<string, string> attribute in attributes)
                {
                    if (attribute.Value == null)
                        continue;

                    result += $" {attribute.Key}=\"{attribute.Value}\"";

                    if (attribute.Key == "href")
                        result += " target=\"_blank\"";
                }
                result += ">";

                // body
                if (text != "")
                    result += text;
                foreach (Element element in elements)
                    result += element.ToString();
                result += $"</{tag}>";

                return result;
            }
        }
    }
}

