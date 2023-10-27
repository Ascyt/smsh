﻿using System;
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
        public string font = "Arial";
        public string favicon = @"https://www.ascyt.com/projects/smsh/favicon.ico";

        private static readonly Dictionary<string, string[]> SHORTHAND_TAGS = new()
        {
            ["uli"] = new string[2] {"ul", "li"},
            ["oli"] = new string[2] {"ol", "li"},
            ["row"] = new string[2] {"table", "tr"},
        };

        private readonly static Dictionary<string, char[]> getAttributes = new()
        {
            ["class"] = new char[2] { '(', ')' },
            ["style"] = new char[2] { '[', ']' },
            ["href"] = new char[2] { '{', '}' },
        };


        public Elements(string markup)
        {
            Section currentSection = new(null, false, null);

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
                        sections.Add(currentSection);

                        string? description = null;
                        Dictionary<string, string> attributes = ExtractAttributes(i, ref line, getAttributes);
                        if (attributes.ContainsKey("class"))
                        {
                            description = attributes["class"];
                        }

                        currentSection = new Section(line.Substring(1).Trim(), true, description);
                        break;
                    case ' ':
                    case '\t':
                        if (line.Length == 0 || line.Trim() == "")
                            break;

                        bool usingCorrect = spaces == 0 ? (line[0] == '\t') : (line[0] == ' ');

                        if (!usingCorrect)
                        {
                            throw new CodeException(spaces == 0 ? $"Invalid indentation (expected tabs, not spaces)" : $"Invalid indentation (expected {spaces} spaces, not tabs)", i, 0);
                        }
                        break;
                    default:
                        TryAddElement(GetElement(line, ref i, 0, null), currentSection?.elements);
                        break;
                }
            }

            if (currentSection != null)
                sections.Add(currentSection);

            void TryAddElement(Element? element, List<Element>? elementList)
            {
                if (element == null || elementList == null)
                {
                    return;
                }

                elementList.Add(element);
            }


            Element? GetElement(string line, ref int i, int indents, Element? parent, Element? shorthandParent = null)
            {
                if (line.TrimStart().Length == 0)
                    return null;

                string trimmedLine = line.Substring(GetIndentIndex(indents));

                if (trimmedLine[0] == '>')
                    return null;

                if ("\t ".Contains(trimmedLine[0]))
                    throw new CodeException($"Invalid indentation (expected {indents}).", i, indents);

                if (!KEYWORD_CHARS.Contains(trimmedLine[0]))
                {
                    return GetElement(line.Insert(GetIndentIndex(indents), ". "), ref i, indents, parent);
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
                        if (sections.Count > 1)
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
                            case "font":
                                font = line.Substring(5).Trim();
                                return null;
                            case "favicon":
                                favicon = line.Substring(8).Trim();
                                return null;
                            default:
                                throw new CodeException("Invalid special tag.", i);
                        }
                    case '$': // Custom class
                        if (sections.Count > 1)
                            throw new CodeException("Custom CSS class must be defined above all sections.", i);

                        customClass = trimmedLine.Substring(1).Trim();

                        if (customClasses.ContainsKey(customClass))
                            throw new CodeException($"Custom CSS class \"{customClass}\" already defined.", i);

                        customClasses[customClass] = "";

                        break;
                    default:
                        break;
                }

                Element? GetThisElement()
                {
                    if (customClass != null)
                    {
                        return null;
                    }

                    Element element = new Element(tag, trimmedLine.Length == tag.Length + 1 ? "" : trimmedLine.Substring(tag.Length + 2), attributes);

                    return element;
                }
                Element? element = GetThisElement();

                i++;

                if (i >= lines.Length)
                    return element;

                int actualIndents = ActualIndents(lines[i]);
                while (actualIndents > indents || lines[i].TrimStart().Length == 0)
                {
                    if (lines[i].Trim().Length > 0)
                    {
                        if (customClass != null)
                        {
                            customClasses[customClass] += lines[i].Trim();
                            break;
                        }

                        TryAddElement(GetElement(lines[i], ref i, actualIndents, element), element.elements);
                    }

                    i++;
                    if (i >= lines.Length)
                        break;

                    actualIndents = ActualIndents(lines[i]);
                }

                if (element != null && shorthandParent == null && SHORTHAND_TAGS.ContainsKey(element.tag))
                {
                    string[] shorthandValues = SHORTHAND_TAGS[element.tag];

                    shorthandParent = new Element(shorthandValues[0], "", attributes);

                    shorthandParent.elements.Add(element);

                    while (i < lines.Length && ActualIndents(lines[i]) == indents)
                    {
                        int start = GetIndentIndex(indents);

                        if (lines[i].Length < start + element.tag.Length + 1 || lines[i].Substring(start, element.tag.Length + 1) != ('.' + element.tag))
                            break;

                        Element? nextElement = GetElement(lines[i], ref i, indents, parent, shorthandParent);

                        if (nextElement == null)
                            break;

                        nextElement.tag = shorthandValues[1];

                        TryAddElement(nextElement, shorthandParent.elements);

                        i++;

                        if (i >= lines.Length)
                            break;

                        actualIndents = ActualIndents(lines[i]);
                    }
                    element.tag = shorthandValues[1];

                    element = shorthandParent;
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

