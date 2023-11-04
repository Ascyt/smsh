using static Program;

namespace Elements
{
    public partial class Elements
    {
        public class Tab
        {
            private int index;
            public string FormattedName => index.ToString();

            public string? name;
            public string? description;
            public List<Section> sections = new List<Section>();

            public Tab(string? name, string? description)
            {
                this.name = name;
                this.description = description;
                
                index = Section.highestIndex;
            }

            public override string ToString()
            {
                if (name == null)
                    return Section.GetSectionsToString(sections);

                string result = $"<section class=\"tab\" id=\"{FormattedName}\">";

                result += Section.GetSectionsToString(sections);

                return result + "</section>";
            }


            public string GetSidebar()
            {
                return $"<div class=\"tabSidebar\" id=\"{FormattedName}\">{Section.GetSidebar(sections)}</div>";
            }
        }
    }
}