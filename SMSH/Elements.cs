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
        public const string KEYWORD_CHARS = ".,>@#:$!~";

        public string file;
        public static Stack<(string, int)> fileStack = new();

        public List<Tab> tabs = new();
        public List<Section> sectionsWithoutTab = new();

        public static string? title;
        public static bool isLightTheme = false;
        public static string toTopText = "Back to top";
        public static bool credit = true;
        public static Dictionary<string, string> customClasses = new();
        public static string font = "Arial";
        public static string favicon = @"https://www.ascyt.com/projects/smsh/favicon.ico";
        public static string? initialHash;
        public static Dictionary<string, Template> templates = new();

        public int spaces; // Amount of spaces used for indentation. 0 = tab

        private string[] lines;
        private Tab? currentTab;
        private Section? currentSection;
        
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

        public Elements(string markup, string fileLocation, int? initialSpaces = null, Tab? initialTab = null)
        {
            file = fileLocation;
            spaces = initialSpaces ?? 0;
            currentTab = initialTab;
            currentSection = new(null, false, null);

            // Go through each line
            lines = markup.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Length == 0)
                    continue;

                switch (line[0])
                {
                    case '@': // Tab
                        if (sectionsWithoutTab.Count > 0)
                            throw new CodeException(this, "When using tabs, elements before tabs are not allowed.", i, 0);

                        if (currentTab != null)
                        {
                            currentTab.sections.Add(currentSection);
                            currentSection = new Section(null, false, null);
                        }

                        currentTab = new Tab(line.Substring(1).Trim(), null);
                        tabs.Add(currentTab);
                        break;
                    case '#': // Section
                        (currentTab?.sections ?? sectionsWithoutTab).Add(currentSection);

                        string? description = null;
                        Dictionary<string, string> attributes = ExtractAttributes(this, i, ref line, getAttributes);
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
                            throw new CodeException(this, spaces == 0 ? $"Invalid indentation (expected tabs, not spaces)" : $"Invalid indentation (expected {spaces} spaces, not tabs)", i, 0);
                        }
                        break;
                    default:
                        TryAddElement(GetElement(lines, line, ref i, 0, null), currentSection?.elements);
                        break;
                }
            }

            if (currentSection != null)
                (currentTab?.sections ?? sectionsWithoutTab).Add(currentSection);


            if (fileStack.Count > 0 && fileStack.Peek().Item1 == file)
                fileStack.Pop();
        }

        private static void TryAddElement(Element? element, List<Element>? elementList)
        {
            if (element == null || elementList == null)
            {
                return;
            }

            elementList.Add(element);
        }

        public Element? GetElement(string[] lines, string line, ref int i, int indents, Element? parent, Element? shorthandParent = null)
        {
            if (line.TrimStart().Length == 0)
                return null;

            string trimmedLine = line.Substring(GetIndentIndex(indents));

            if (trimmedLine[0] == '>')
                return null;

            if ("\t ".Contains(trimmedLine[0]))
                throw new CodeException(this, $"Invalid indentation (expected {indents}).", i, indents);

            if (!KEYWORD_CHARS.Contains(trimmedLine[0]))
            {
                return GetElement(lines, line.Insert(GetIndentIndex(indents), ". "), ref i, indents, parent);
            }


            Dictionary<string, string> attributes = ExtractAttributes(this, i, ref trimmedLine, getAttributes);

            string tag = trimmedLine.Split(' ')[0];
            if (trimmedLine[0] != '~')
                trimmedLine = FormatText(this, trimmedLine, indents, i);

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
                    throw new CodeException(this, "Image must have a link attribute.", i);

                imageElement.attributes["src"] = imageElement.attributes["href"];
                imageElement.attributes.Remove("href");

                return imageElement;
            }

            string? customClass = null;
            Template? template = null;

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
                case '!': // File
                    string file = line.Substring(1).Trim();
                    if (Path.GetExtension(file) != ".smsh")
                    {
                        if (Path.HasExtension(file))
                            throw new CodeException(this, $"Invalid file extension: {Path.GetExtension(file)}", i, indents);
                        file += ".smsh";
                    }

                    if (!File.Exists(file))
                        throw new CodeException(this, $"File not found: {file}", i, indents);

                    if (fileStack.Any(x => x.Item1 == file))
                        throw new CodeException(this, $"Circular file reference: {file}", i, 0);

                    string fileText = Program.ReadFile(file);

                    (string, int) newStackElement = (file, i);
                    fileStack.Push(newStackElement);

                    Elements elements = new(fileText, file, spaces, currentTab);

                    if (elements.tabs.Count == 0)
                    {
                        return null;
                    }

                    tabs.AddRange(elements.tabs);

                    currentTab = elements.tabs.Last();
                    if (currentTab.sections.Count > 0)
                    {
                        currentSection = currentTab.sections.Last();
                        tabs.Last().sections.RemoveAt(tabs.Last().sections.Count - 1);
                    }

                    return null;
                case '#': // Section
                    throw new CodeException(this, "Sections can't be defined in elements.", i, indents);
                case '@': // Tab
                    throw new CodeException(this, "Tabs can't be defined in elements.", i, indents);
                case ':': // Special tag
                    string specialTag = line.Split(' ')[0].Substring(1).ToLower();

                    if ((currentTab?.sections ?? sectionsWithoutTab).Count > 0 && specialTag != "spaces")
                        throw new CodeException(this, "Special tag must be defined above all sections and tabs.", i);

                    switch (specialTag)
                    {
                        case "title":
                            if (title != null)
                                throw new CodeException(this, "Title already defined.", i);

                            title = line.Substring(7).Trim();
                            return null;
                        case "theme":
                            string[] light = {"light", "l"};
                            string[] dark = {"dark", "d"};

                            string theme = line.Substring(6).Trim().ToLower();
                            bool isLight = light.Contains(theme);
                            bool isDark = dark.Contains(theme);

                            if (!isLight && !isDark)
                                throw new CodeException(this, "Invalid theme. (Valid options: light, dark)", i, line.Length - theme.Length);

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
                        case "initialhash":
                            initialHash = line.Substring(12).Trim();
                            return null;    
                        default:
                            throw new CodeException(this, "Invalid special tag.", i);
                    }
                case '$': // Custom class
                    if ((currentTab?.sections ?? sectionsWithoutTab).Count > 0)
                        throw new CodeException(this, "Custom CSS class must be defined above all sections and tabs.", i);

                    customClass = trimmedLine.Substring(1).Trim();

                    if (customClasses.ContainsKey(customClass))
                        throw new CodeException(this, $"Custom CSS class \"{customClass}\" already defined.", i);

                    customClasses[customClass] = "";

                    break;
                case '~': // Template
                    switch (trimmedLine[1])
                    {
                        case '+':
                            string[] templateSplit = trimmedLine.Substring(2).Trim().Split(' ');
                            string templateName = templateSplit[0];
                            templateSplit = templateSplit.Skip(2).ToArray();
                            if (templates.ContainsKey(templateName))
                                throw new CodeException(this, $"Template \"{templateName}\" already defined.", i);

                            string templateContent = "";
                            i++;
                            while (i < lines.Length && ActualIndents(lines[i]) > indents)
                            {
                                if (i >= lines.Length)
                                    break;
                                templateContent += lines[i].Substring(GetIndentIndex(indents + 1)) + "\n";
                                i++;
                            }
                            i--;

                            if (templateContent == "")
                                throw new CodeException(this, $"Template \"{templateName}\" has no content.", i);

                            template = new Template(this, templateName, templateSplit, templateContent);
                            templates[templateName] = template;
                            return null;
                        case '-':
                            string templateName1 = trimmedLine.Substring(2).Trim();
                            if (!templates.Remove(templateName1))
                                throw new CodeException(this, $"Template \"{templateName1}\" not found.", i);
                            return null;
                        default:
                            string templateName2 = trimmedLine.Split(' ')[0].Substring(1);
                            string[] templateSplit2 = trimmedLine.Substring(4 + templateName2.Length).Trim().Split('>');
                            // Trim all the templateSplit2 elements
                            for (int j = 0; j < templateSplit2.Length; j++)
                                templateSplit2[j] = templateSplit2[j].Trim();

                            if (!templates.ContainsKey(templateName2))
                                throw new CodeException(this, $"Template \"{templateName2}\" not found.", i);
                            return templates[templateName2].FormatContent(templateSplit2);
                    }
                default:
                    break;
            }

            Element? GetThisElement()
            {
                if (customClass != null || template != null)
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
                    if (template != null)
                    {
                        template.content += lines[i].Substring(GetIndentIndex(indents)).TrimEnd();
                        break;
                    }

                    TryAddElement(GetElement(lines, lines[i], ref i, actualIndents, element), element.elements);
                }

                i++;
                if (i >= lines.Length)
                    break;

                actualIndents = ActualIndents(lines[i]);
            }
            if (template != null)
            {
                templates[template.name] = template;
                return null;
            }

            // Shorthands
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

                    Element? nextElement = GetElement(lines, lines[i], ref i, indents, parent, shorthandParent);

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

        public int GetIndentIndex(int indents)
            => spaces == 0 ? indents : indents * spaces;
    }
}

