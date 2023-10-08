using static Program;

namespace Elements
{
    public partial class Elements
    {
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
    }
}