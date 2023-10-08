using static Program;

namespace Elements
{
    public partial class Elements
    {
        public class Section
        {
            public static int highestIndex = -1;
            private int index;
            public string FormattedName => index.ToString();

            public string name;
            public List<Element> elements = new List<Element>();
            public bool withTitle;

            public Section(string name, bool withTitle)
            {
                this.name = name;
                this.withTitle = withTitle;
                
                index = ++highestIndex;
            }

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

    }
}