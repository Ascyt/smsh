using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Program;

namespace Elements  
{
    public partial class Elements
    {
        public static string FormatText(Elements elements, string text, int indents, int lineIndex)
        {
            text = text.Replace("\\\\", "&#92;").Replace("\\<", "&lt;").Replace("\\>", "&gt;");

            string result = "";
            string currentFormat = "";
            int formattingDelta = 0;
            const char FORMAT_START = '<';
            const char FORMAT_END = '>';
            Template? template = null;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == FORMAT_START)
                {
                    formattingDelta++;

                    if (formattingDelta == 1)
                    {
                        if (text[i + 1] == '~')
                        {
                            string templateName = "";
                            i++;
                            while (text[++i] != ' ')
                            {
                                templateName += text[i];
                            }

                            if (!templates.TryGetValue(templateName, out template))
                            {
                                throw new CodeException(elements, $"Template {templateName} not found.", lineIndex, indents, i);
                            }
                            formattingDelta -= template.parameters.Length + 1;
                            i++;
                        }
                        continue;
                    }
                }

                if (template != null)
                {
                    currentFormat += text[i];
                }

                if (text[i] == FORMAT_END)
                {
                    if (template != null)
                    {
                        formattingDelta++;

                        if (formattingDelta == 0)
                        {
                            currentFormat = currentFormat.Substring(0, currentFormat.Length - 1);
                            string[] parameterValues = currentFormat.Split('>');
                            for (int j = 0; j < parameterValues.Length; j++)
                                parameterValues[j] = parameterValues[j].Trim();

                            Element? element = template.FormatContent(currentFormat.Split('>'));
                            string value = element?.ToString() ?? "";
                            // Get rid of <br> at the end if exists
                            if (value.EndsWith("<br></span></span>"))
                            {
                                value = value.Substring(0, value.Length - 18);
                                value += "</span></span>";
                            }
                            result += value;
                            
                            template = null;
                            currentFormat = "";
                        }

                        continue;
                    }

                    formattingDelta--;

                    if (formattingDelta < 0)
                        throw new CodeException(elements, $"Unexpected format closing tag.", lineIndex, indents, i);

                    if (formattingDelta == 0)
                    {
                        if (new string[] { "br", "hr", "img", "input", "meta", "link" }.Contains(currentFormat.Trim()))
                        {
                            result += $"<{currentFormat}>";
                        }
                        else
                        {
                            Dictionary<string,string> attributes = ExtractAttributes(elements, lineIndex, ref currentFormat, getAttributes);

                            string formatTag = currentFormat.Split(' ')[0];

                            currentFormat = FormatText(elements, currentFormat.Substring(formatTag.Length).TrimStart(), indents, lineIndex);

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

                if (template == null)
                    result += text[i];
            }

            if (formattingDelta > 0)
                throw new CodeException(elements, "Unclosed format tag.", lineIndex, indents, text.Length);
                                                                                                                                                                                                                                                                                                                                result = result.Replace(" Ascyt ", " <b style=\"color:gold;text-shadow:1px 1px 0px orange\">Ascyt</b> "); // sshhh...
            return result;
        }
    }
}
