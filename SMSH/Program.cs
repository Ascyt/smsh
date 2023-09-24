using System;
using System.IO;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Xml;

public class Program
{
    public class CodeException : Exception
    {
        public int line;
        public CodeException(string message, int? line = null) : base(message) 
        {
            this.line = line ?? -1;
        }
    }

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: No file path specified.");
            return;
        }

        try
        {
            string filePath = args[0];
            string outputPath = args.Length > 2 && args[1] == "-o" ? args[2] : Path.ChangeExtension(filePath, ".html");


            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            string markup = File.ReadAllText(filePath).Replace("\r", "");

            File.WriteAllText(outputPath, FormatHTML(markup, HTML));

            Console.WriteLine($"File has been converted to HTML at: {Path.GetFullPath(outputPath)}");
        }
        catch (NotImplementedException e) { throw e; } // Just so I can quickly comment out the other exceptions
        catch (CodeException e)
        {
            Console.WriteLine(e.line == -1 ? $"Error:\n{e.Message}" : $"Error on line {e.line + 1}:\n{e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error:\n{e.Message}");
        }
    }

    public static string FormatHTML(string markup, string html)
    {
        Elements.Elements elements = new(markup);
        
        string title = elements.title ?? "Untitled";
        string sidebar = "";
        foreach (Elements.Elements.Section section in elements.sections)
        {
            sidebar += $"<li><a href=\"#{section.FormattedName}\">{section.name}</a></li>";
        }

        string header = elements.header?.ToString() ?? "";
        string body = "";
        foreach (Elements.Elements.Section section in elements.sections)
        {
            body += section.ToString();
        }

        return html.Replace("{{TITLE}}", title)
            .Replace("{{SIDEBAR}}", sidebar)
            .Replace("{{HEADER}}", header)
            .Replace("{{BODY}}", body);
    }

    // minified html
    const string HTML =
        @"<!doctypehtml><html lang=""en""><meta charset=""UTF-8""><meta content=""width=device-width,initial-scale=1""name=""viewport""><title>{{TITLE}}</title><div id=""wrapper""><nav id=""sidebar""><ul>{{SIDEBAR}}</ul></nav><div id=""content""><div id=""header""><section>{{HEADER}}</section></div>{{BODY}} <a href=""#""id=""back-to-top"">Back to top</a></div></div><style>html{scroll-behavior:smooth}body{font-family:'Gill Sans','Gill Sans MT',Calibri,'Trebuchet MS',sans-serif;font-size:large;margin:10px;text-align:left;background-color:#000;color:silver;line-height:28px}section{border-top-style:solid;border-top-width:3px;border-color:#101010;padding-top:10px;margin-bottom:10px}.light{color:#fff}.dark{color:grey}.card{border-color:grey;border-radius:5px;border-width:2px;border-style:solid;background-color:#080808;padding:10px;margin-top:10px;margin-bottom:10px}.card.dashed{border-style:dashed}a{color:#40a6ff;transition:ease-out .3s}a:hover{color:#2f7cbf;transition:ease-in-out .2s}li{margin-left:-10px}li>ul,ol>li{margin-left:-15px}table{width:100%;border-collapse:collapse;margin-top:10px;margin-bottom:10px}th{background-color:#202020;color:#fff}td,th{padding:8px;border:1px solid #404040;border-style:solid;text-align:center}tr:nth-child(odd){background-color:#101010}.invis>td{border-style:none}code,pre{white-space:pre-wrap;overflow-x:auto;color:silver;background-color:#101010;border-color:grey;border-width:1px;border-style:solid;border-right-style:solid;padding:10px;border-radius:5px}code{padding:2px;padding-left:3px;padding-right:3px}#content{flex:1;padding:10px;padding-left:5px}#content h1{color:#fff;text-align:center;margin-top:0;margin-bottom:10px}#content h2{color:#fff;text-decoration:underline;font-size:larger}#content h3{color:#fff;text-decoration:underline;font-size:large}#content h4{color:#fff;text-decoration:underline;font-size:large;font-weight:400}#header{text-align:center}#header .t,#header h1{margin-bottom:-15px}#header .t{font-size:larger}#header h1{font-size:xx-large}#header section{margin-bottom:30px}#wrapper{display:flex}#sidebar{width:15%;color:#fff}#sidebar ul{list-style-type:none;padding:0;position:fixed;width:15%;margin-top:-10px;margin-left:-10px;max-height:100vh;overflow-y:auto}#sidebar ul li a{color:#fff;text-decoration:none;display:flex;padding-bottom:10px;padding-top:10px;padding-left:10px;justify-content:center}#sidebar ul li:nth-child(even){background-color:#101010;transition:ease-out .15s}#sidebar ul li:nth-child(even):hover{background-color:#303030;transition:ease-in-out .1s}#sidebar ul li:nth-child(odd){background-color:#202020;transition:ease-out .15s}#sidebar ul li:nth-child(odd):hover{background-color:#303030;transition:ease-in-out .1s}#back-to-top{border-color:grey;border-radius:5px;border-width:2px;border-style:solid;margin-top:10px;display:flex;justify-content:center;background-color:#101010;color:silver;text-decoration:none;transition:ease-out .3s}#back-to-top:hover{background-color:#303030;transition:ease-in-out .2s}</style>";
}