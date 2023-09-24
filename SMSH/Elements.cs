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
        public List<Section> sections = new List<Section>();
        public Element? header;
        public string? title;

        public Elements(string markup)
        {
            Section? currentSection = null;

            // Go through each line
            string[] lines = markup.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Trim() == "") // Empty line
                    continue;


                switch (line[0])
                {
                    case '#': // Section
                        if (currentSection != null)
                            sections.Add(currentSection);

                        bool withTitle = line[1] != '#';
                        string sectionName = line.Substring(withTitle ? 1 : 2).Trim();

                        currentSection = new Section(sectionName, withTitle);
                        break;
                    case ':': // Special tag
                        if (currentSection != null)
                            throw new CodeException("Special tag must be defined above all sections.", i);

                        switch (line.Split(' ')[0])
                        {
                            case ":title":
                                if (title != null)
                                    throw new CodeException("Title already defined.", i);

                                title = line.Substring(7).Trim();
                                break;
                            case ":header":
                                if (header != null)
                                    throw new CodeException("Header already defined.", i);

                                header = GetElement(line, ref i, 1);
                                break;
                            default:
                                throw new CodeException("Invalid special tag.", i);
                        }
                        break;
                    case '\t':
                        if (line.Trim() != "")
                            throw new CodeException("Too large amount of indentation (expected none).", i);
                        break;
                    case ' ':
                        throw new CodeException("Invalid indentation (expected tab, not space).", i);
                    case '>': // Comment
                        break;

                    case ',': // Margin
                    case '.': // Tag
                        if (currentSection == null)
                            throw new CodeException("No section defined.", i);
                        currentSection.elements.Add(GetElement(line, ref i, 1));
                        break;
                    default:
                        if (currentSection == null)
                            throw new CodeException("No section defined.", i);
                        currentSection.elements.Add(GetElement(". " + line, ref i, 1));
                        break;
                }
            }

            if (currentSection != null)
                sections.Add(currentSection);

            Element GetElement(string line, ref int i, int indents)
            {
                string trimmedLine = line.TrimStart();

                if (trimmedLine[0] == ',') // Margin
                {
                    string amount = trimmedLine.Substring(1).Trim();
                    if (amount == "")
                        amount = "16px";

                    return new Element(".div", "", new Dictionary<string, string>()
                    {
                        {"style", $"margin-bottom:{amount};"},
                    });
                }

                string tag = trimmedLine.Split(' ')[0];

                if (tag.Length >= 4 && tag.Substring(0, 4) == ".img")
                {
                    string alt = trimmedLine.Substring(tag.Length).Trim();
                    if (alt == "")
                        alt = "Image";

                    Dictionary<string, string> keyValuePairs = Element.ExtractAttributes(ref tag, Element.getAttributes);

                    keyValuePairs["alt"] = alt;

                    Element element = new Element(tag, "", keyValuePairs);

                    if (!element.attributes.ContainsKey("href"))
                        throw new CodeException("Image must have a link attribute.", i);

                    string src = element.attributes["href"];
                    element.attributes["src"] = src;
                    element.attributes.Remove("href");

                    return element;
                }

                if (trimmedLine.Substring(tag.Length).TrimEnd() == "")
                {
                    Element element = new Element(tag);
                    i++;

                    if (i >= lines.Length)
                        return element;

                    int actualIndents = ActualIndents(lines[i]);

                    if (actualIndents != indents)
                        throw new CodeException($"Incorrect amount of indentation (expected {indents}, got {actualIndents}).", i);

                    while (actualIndents >= indents)
                    {
                        if (actualIndents != lines[i].Length) // Not only whitespaces
                        {
                            if (actualIndents > indents)
                                throw new CodeException($"Too large amount of indentation (expected {indents}, got {actualIndents}).", i);

                            string newElementTag = lines[i].TrimStart().Split(' ')[0];

                            switch (newElementTag[0])
                            {
                                case ':': // Special tag
                                case '.': // Tag
                                case ',': // Margin
                                    if (element.text != "")
                                        throw new CodeException("The same element cannot have both text and more elements.", i);

                                    element.elements.Add(GetElement(lines[i], ref i, indents + 1));
                                    break;
                                case '>': // Comment
                                    break;
                                case '\\':
                                default: // Text
                                    if (element.elements.Count > 0)
                                        throw new CodeException("The same element cannot have both text and more elements.", i);

                                    element.text += lines[i].Substring(indents + (newElementTag[0] == '\\' ? 1 : 0));
                                    if (tag == ".pre")
                                        element.text += "<br>";
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
                }

                return new Element(tag, line.Substring(indents + tag.Length).Trim() + "<br>");

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

        public class Section
        {
            public string name;
            public List<Element> elements = new List<Element>();

            public bool withTitle;

            public Section(string name, bool withTitle)
            {
                this.name = Element.FormatText(name);
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
            public readonly static Dictionary<string, char[]> getAttributes = new Dictionary<string, char[]>()
            {
                {"class", new char[2] { '(', ')' }},
                {"style", new char[2] { '[', ']' }},
                {"href", new char[2] { '{', '}' }},
            };
            public string text;
            public List<Element> elements = new List<Element>();

            public Element(string tag, string text = "", Dictionary<string, string>? attributes = null)
            {
                if (tag[0] == '.' || tag[0] == ':')
                    tag = tag.Substring(1);

                this.attributes = attributes ?? ExtractAttributes(ref tag, getAttributes);

                if (new string[] { "header", "card" }.Contains(tag))
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
            public static Dictionary<string, string> ExtractAttributes(ref string tag, Dictionary<string, char[]> attributes)
            {
                string newTag = "";
                Dictionary<string, string> result = new Dictionary<string, string>();

                string? inAttribute = null;
                bool hasChangedAttribute = false;

                for (int i = 0; i < tag.Length; i++)
                {
                    if (TryChangeAttribute(tag[i]))
                    {
                        hasChangedAttribute = true;

                        continue;
                    }

                    if (inAttribute != null)
                    {
                        if (!result.ContainsKey(inAttribute))
                            result[inAttribute] = "";
                        result[inAttribute] += tag[i];

                        continue;
                    }

                    if (hasChangedAttribute)
                    {
                        // TODO: Add add line and column=index number to exception
                        throw new CodeException($"Unexpected character outside of attribute: '{tag[i]}'.");
                    }

                    newTag += tag[i];
                }

                tag = newTag;
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
                            throw new CodeException($"Unexpected attribute closing character: '{c}'.");
                        }
                    }
                    return false;
                }
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

                string formattedText = FormatText(text);

                // body
                if (text != "")
                    result += formattedText;
                foreach (Element element in elements)
                    result += element.ToString();
                result += $"</{tag}>";

                return result;
            }

            public static string FormatText(string text)
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

                        if (formattingDelta < 0) // TODO: Add line and column=index number to exception
                            throw new CodeException($"Unexpected format closing tag in line \"{text}\".");

                        if (formattingDelta == 0)
                        {
                            if (new string[] { "br", "hr", "img", "input", "meta", "link" }.Contains(currentFormat.Trim()))
                            {
                                result += $"<{currentFormat}>";
                            }
                            else
                            {
                                string formatTag = currentFormat.Split(' ')[0];
                                Element element = new Element(formatTag, currentFormat.Substring(formatTag.Length).Trim());
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
                    throw new CodeException("Unclosed format tag.");

                return result;
            }
        }
    }
}
