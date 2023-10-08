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
        public static string FormatText(string text, int indents, int lineIndex)
        {
            text = text.Replace("\\\\", "&#92;").Replace("\\<", "&lt;").Replace("\\>", "&gt;");

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
                                                                                                                                                                                                                                                                                                                                result = result.Replace(" Ascyt ", " <b style=\"color:gold;text-shadow:1px 1px 0px orange\">Ascyt</b> "); // sshhh...
            return result;
        }
    }
}
