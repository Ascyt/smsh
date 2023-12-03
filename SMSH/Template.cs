using static Program;

namespace Elements
{
    public partial class Elements
    {
        public class Template
        {
            public readonly string name;
            public readonly string[] parameters;
            public string content;

            public Template(string name, string[] parameters)
            {
                this.name = name;
                this.parameters = parameters;
            }

            public Element FormatContent(string[] parameterValues)
            {
                string result = "";

                throw new NotImplementedException();
            }
        }
    }
}