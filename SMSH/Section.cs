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

            public string? name;
            public string? description;
            public List<Element> elements = new List<Element>();
            public bool withTitle;

            public Section(string? name, bool withTitle, string? description)
            {
                this.name = name;
                this.withTitle = withTitle;
                this.description = description;
                
                index = ++highestIndex;
            }

            public override string ToString()
            {
                if (name == null)
                    return Element.GetElementsToString(elements);

                string result = $"<section class=\"section\" id=\"{FormattedName}\">";

                if (withTitle)
                    result += $"<h1{(description == null ? " style=\"margin-bottom:10px\"" : "")}>{name}</h1>" +
                        (description != null ? $"<div class=\"sectionDesc\">{description}</div>" : "");

                result += Element.GetElementsToString(elements);

                return result + "</section>";
            }
            public static string GetSectionsToString(List<Section> sections)
            {
                string result = "";
                
                foreach (Section section in sections)
                    result += section.ToString();

                return result;
            }

            public static string GetSidebar(List<Section> sections)
            {
                string sidebar = "";
                foreach (Section section in sections)
                {
                    if (section.name == null)
                        continue;

                    sidebar += $"<li class=\"sidebar-item\" data-link-to=\"{section.FormattedName}\">" +
                        $"<a href=\"#{section.FormattedName}\" {(section.description != null ? $"title=\"{section.description}\" " : "")}onclick=\"sectionClick('{section.FormattedName}')\">{section.name}" +
                        $"</a></li>";
                }
                return sidebar;
            }
        }
    }
}