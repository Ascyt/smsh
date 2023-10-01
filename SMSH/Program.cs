using System;
using System.IO;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Xml;

public class Program
{
    public class CodeException : Exception
    {
        public int line;
        public int? column;

        public CodeException(string message, int line, int? indents = null, int? column = null) : base(message) 
        {
            this.line = line;
            if (column != null || indents != null)
                this.column = (column ?? 0) + (indents ?? 0);
        }

        public override string ToString()
        {
            return $"Error on line {line + 1}{(column != null ? $" at column {column + 1}" : "")}:\n\t{Message}";
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
        /*catch (CodeException e)
        {
            Console.WriteLine(e);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error:\n{e.Message}");
        }*/
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

        string body = "";
        foreach (Elements.Elements.Section section in elements.sections)
        {
            body += section.ToString();
        }
        
        Regex regex = new(@"{{#(.*?)}}", RegexOptions.Compiled);

        html = regex.Replace(html, m => {
            string color = m.Groups[1].Value;

            if (elements.isLightTheme) // Set hex to 0x100 - value 
            {
                color = (0x100 - int.Parse(color, System.Globalization.NumberStyles.HexNumber)).ToString("X");
            }

            return $"#{color}{color}{color}";
            });


        return html.Replace("{{TITLE}}", title)
            .Replace("{{SIDEBAR}}", sidebar)
            .Replace("{{BODY}}", body)
            .Replace("{{TO_TOP_TEXT}}", elements.toTopText);
    }

    // minified html
    const string HTML =
        @"<!DOCTYPE html><html lang=en><meta charset=UTF-8><meta content=""width=device-width,initial-scale=1""name=viewport><title>{{TITLE}}</title><div id=wrapper><nav id=sidebar><ul>{{SIDEBAR}}</ul></nav><div id=content>{{BODY}} <a href=# id=back-to-top>{{TO_TOP_TEXT}}</a></div></div><style>html{scroll-behavior:smooth}body{font-family:'Gill Sans','Gill Sans MT',Calibri,'Trebuchet MS',sans-serif;font-size:large;margin:10px;text-align:left;background-color:{{#00}};color:{{#c0}};line-height:28px}section{border-top-style:solid;border-top-width:3px;border-color:{{#10}};padding-top:10px;margin-bottom:10px}.light{color:{{#ff}}}.dark{color:{{#80}}}.card{border-color:{{#80}};border-radius:5px;border-width:2px;border-style:solid;background-color:{{#08}};padding:10px;margin-top:10px;margin-bottom:10px}.card.dashed{border-style:dashed}a{color:#40a6ff;transition:ease-out .3s}a:hover{color:#2f7cbf;transition:ease-in-out .2s}li{margin-left:-10px}li>ul,ol>li{margin-left:-15px}table{width:100%;border-collapse:collapse;margin-top:10px;margin-bottom:10px}th{background-color:{{#20}};color:{{#ff}}}td,th{padding:8px;border:1px solid {{#40}};border-style:solid;text-align:center}tr:nth-child(odd){background-color:{{#10}}}.invis>td{border-style:none}code,pre{white-space:pre-wrap;overflow-x:auto;color:{{#c0}};background-color:{{#10}};border-color:{{#80}};border-width:1px;border-style:solid;border-right-style:solid;padding:10px;border-radius:5px}code{padding:2px;padding-left:3px;padding-right:3px}#content{flex:1;padding:10px;padding-left:5px}#content h1{color:{{#ff}};text-align:center;margin-top:0;margin-bottom:10px}#content h2{color:{{#ff}};text-decoration:underline;font-size:larger}#content h3{color:{{#ff}};text-decoration:underline;font-size:large}#content h4{color:{{#ff}};text-decoration:underline;font-size:large;font-weight:400}#wrapper{display:flex}#sidebar{width:15%;color:{{#ff}}}#sidebar ul{list-style-type:none;padding:0;position:fixed;width:15%;margin-top:-10px;margin-left:-10px;max-height:100vh;overflow-y:auto}#sidebar ul li a{color:{{#ff}};text-decoration:none;display:flex;padding-bottom:10px;padding-top:10px;padding-left:10px;justify-content:center}#sidebar ul li:nth-child(even){background-color:{{#10}};transition:ease-out .15s}#sidebar ul li:nth-child(even):hover{background-color:{{#30}};transition:ease-in-out .1s}#sidebar ul li:nth-child(odd){background-color:{{#20}};transition:ease-out .15s}#sidebar ul li:nth-child(odd):hover{background-color:{{#30}};transition:ease-in-out .1s}#back-to-top{border-color:{{#80}};border-radius:5px;border-width:2px;border-style:solid;margin-top:10px;display:flex;justify-content:center;background-color:{{#10}};color:{{#c0}};text-decoration:none;transition:ease-out .3s}#back-to-top:hover{background-color:{{#30}};transition:ease-in-out .2s}</style>";
}