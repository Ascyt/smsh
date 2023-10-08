using static Program;

namespace Elements
{
    public partial class Elements
    {
        public class Element
        {
            public string tag;
            public readonly Dictionary<string, string> attributes = new Dictionary<string, string>();
            
            public string text;
            public List<Element> elements = new List<Element>();

            public Element(string tag, string text = "", Dictionary<string, string>? attributes = null)
            {
                if (tag.Length > 0 && tag[0] == '.')
                    tag = tag.Substring(1);

                this.attributes = attributes ?? new Dictionary<string, string>();

                if (new string[] { "card" }.Contains(tag))
                {
                    if (this.attributes.ContainsKey("class"))
                        this.attributes["class"] += " " + tag;
                    else
                        this.attributes["class"] = tag;
                    this.tag = "div";
                }
                else
                {
                    if (tag == "")
                    {
                        if (this.attributes.ContainsKey("class"))
                            this.attributes["class"] = "t " + this.attributes["class"];
                        else
                            this.attributes["class"] = "t";

                        this.tag = "span";
                    }
                    else
                        this.tag = tag;
                }

                this.text = text;
            }
           
            public override string ToString()
            {
                string result = $"<{tag}";

                // attributes
                foreach (KeyValuePair<string, string> attribute in attributes)
                {
                    if (attribute.Value == null)
                        continue;

                    result += $" {attribute.Key}=\"{attribute.Value}\"";

                    if (attribute.Key == "href" && attribute.Value[0] != '#')
                        result += " target=\"_blank\"";
                }
                result += ">";

                // body
                if (text != "")
                    result += text;
                foreach (Element element in elements)
                    result += element.ToString();
                result += $"</{tag}>";

                return result;
            }
        }
    }
}