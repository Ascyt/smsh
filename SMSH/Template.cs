using static Program;

namespace Elements
{
    public partial class Elements
    {
        public class Template
        {
            public Elements elements;
            public readonly string name;
            public readonly string[] parameters;
            private readonly string[] sortedParameters;
            public string content;

            public Template(Elements elements, string name, string[] parameters, string content)
            {
                this.elements = elements;
                this.name = name;
                this.parameters = parameters.ToArray();
                this.sortedParameters = parameters.ToArray();
                // Throw error when there are duplicate parameters
                if (parameters.Length != parameters.Distinct().Count())
                {
                    throw new CodeException(elements, "Duplicate parameters in template definition " + name, -1);
                }
                // Make sure the parameters are sorted by length, so that the longest ones are matched first
                Array.Sort(this.sortedParameters, (a, b) => b.Length.CompareTo(a.Length));
                this.content = content.Replace("\\>", "&gt;");
            }

            public Element? FormatContent(string[] parameterValues)
            {
                if (parameterValues.Length != parameters.Length)
                {
                    throw new CodeException(elements, $"Incorrect number of parameters for template {name} (expected {parameters.Length}, got {parameterValues.Length})", -1);
                }

                string result = content;
                for (int i = 0; i < sortedParameters.Length; i++)
                {
                    int index = Array.IndexOf(parameters, sortedParameters[i]);

                    result = result.Replace(">" + parameters[index], parameterValues[index]);
                }

                string[] resultSplit = result.Split('\n');
                int elementsI = 0;

                return elements.GetElement(resultSplit, resultSplit[0], ref elementsI, 0, null);
            }
        }
    }
}