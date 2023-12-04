using System;
using System.IO;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Xml;
using Elements;

public class Program
{
    public class CodeException : Exception
    {
        public int line;
        public int? column;
        public Elements.Elements elements;

        public CodeException(Elements.Elements elements, string message, int line, int? indents = null, int? column = null) : base(message) 
        {
            this.line = line;
            if (column != null || indents != null)
                this.column = (column ?? 0) + elements.GetIndentIndex(indents ?? 0);

            this.elements = elements;
        }

        public void Print()
        {
            WriteColor("Error in file ", ConsoleColor.Gray);
            WriteColor(elements.file, ConsoleColor.DarkYellow);
            WriteColor(", line ", ConsoleColor.Gray);
            WriteColor((line + 1).ToString(), ConsoleColor.Yellow); 
            WriteColor(column != null ? $", column " : "", ConsoleColor.Gray);
            WriteColor((column + 1)?.ToString(), ConsoleColor.Yellow);
            WriteColor(":\n\t", ConsoleColor.Gray);
            WriteColor(Message, ConsoleColor.Red);

            if (Elements.Elements.fileStack.Count == 0)
                return;

            WriteColor("\n\nStack (excluding current file):", ConsoleColor.Gray);
                
            foreach ((string, int) stack in Elements.Elements.fileStack)
            {
                WriteColor($"\n\t{stack.Item1}", ConsoleColor.DarkYellow);
                WriteColor(", line ", ConsoleColor.Gray);
                WriteColor((stack.Item2 + 1).ToString(), ConsoleColor.Yellow);
            }
        }
    }

    public const bool USE_BOILERPLATE_FILE = false;
    public static string currentHtml = "";

    public static void WriteColor(string? text, ConsoleColor color)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    public static void Main(string[] args)
    {
        currentHtml = USE_BOILERPLATE_FILE ? File.ReadAllText("./boilerplate.html") : HTML;

        if (args.Length == 0)
        {
            WriteColor("Error: No file path specified.", ConsoleColor.Red);
            return;
        }
        try
        {
            string filePath = args[0];
            if (Path.GetExtension(filePath) != ".smsh")
            {
                if (Path.HasExtension(filePath))
                {
                    WriteColor("Error: File must end in \".smsh\".", ConsoleColor.Red);
                    return;
                }
                filePath += ".smsh";
            }

            string outputPath = args.Length > 2 && args[1] == "-o" ? args[2] : Path.ChangeExtension(filePath, ".html");

            if (!File.Exists(filePath))
            {
                WriteColor($"File not found: {filePath}", ConsoleColor.Red);
                return;
            }

            string markup = ReadFile(filePath); 

            File.WriteAllText(outputPath, FormatHTML(markup, filePath));

            WriteColor($"File has been compiled to HTML at: ", ConsoleColor.Gray);
            WriteColor(Path.GetFullPath(outputPath).ToString(), ConsoleColor.Green);
        }
        catch (NotImplementedException e) { throw e; } // Just so I can quickly comment out the other exceptions
        catch (CodeException e)
        {
            e.Print();
        }/*
        catch (Exception e)
        {
            Console.WriteLine($"Error:\n{e.Message}");
        }*/
    }
    public static string ReadFile(string filePath)
    {
        return File.ReadAllText(filePath).Replace("\r", "") + '\n';
    }

    public static string FormatHTML(string markup, string fileName)
    {
        string html = currentHtml;

        Elements.Elements elements = new(markup, fileName);
        
        string title = Elements.Elements.title ?? "Untitled";
        string body = "";
        string sidebar = "";
        string navbar = "";

        foreach (Elements.Elements.Tab tab in elements.tabs)
        {
            body += tab.ToString();
            sidebar += tab.GetSidebar();
            navbar += $"<li class=\"navbar-item\" id={tab.FormattedName} data-link-to=\"{tab.FormattedName}\"><a href=\"#{tab.FormattedName}\" onclick=\"tabClick('{tab.FormattedName}')\">{tab.name}</a></li>";
        }

        if (elements.tabs.Count == 0)
        {
            body += Elements.Elements.Section.GetSectionsToString(elements.sectionsWithoutTab);
            sidebar += Elements.Elements.Section.GetSidebar(elements.sectionsWithoutTab);
        }
        
        Regex regex = new(@"{{#(.*?)}}", RegexOptions.Compiled);

        html = regex.Replace(html, m => {
            string color = m.Groups[1].Value;

            if (Elements.Elements.isLightTheme) // Set hex to 0x100 - value 
            {
                color = (0x100 - int.Parse(color, System.Globalization.NumberStyles.HexNumber)).ToString("X");
            }

            return $"#{color}{color}{color}";
            });

        string customStyles = "";   
        foreach (KeyValuePair<string, string> customClass in Elements.Elements.customClasses)
        {
            customStyles += $".{customClass.Key}{{{customClass.Value}}}";
        }


        return html.Replace("{{TITLE}}", title)
            .Replace("{{SIDEBAR}}", sidebar)
            .Replace("{{NAVBAR}}", navbar)
            .Replace("{{BODY}}", body)
            .Replace("{{TO_TOP_TEXT}}", Elements.Elements.toTopText)
            .Replace("{{CREDIT}}", Elements.Elements.credit ? CREDIT : "")
            .Replace("{{CUSTOM_STYLES}}", customStyles)
            .Replace("{{FONT}}", Elements.Elements.font)
            .Replace("{{INITIAL_HASH}}", Elements.Elements.initialHash ?? (elements.tabs.Count == 0 ? elements.sectionsWithoutTab[0].FormattedName : elements.tabs[0].FormattedName))
            .Replace("{{FAVICON}}", Elements.Elements.favicon);
    }

    // minified credit
    const string CREDIT = @"<div id=credit><span class=""low t"" id=""credit"">This file has been generated using SMSH, a fast, easy and open-source markup language used to write a pretty and structured HTML document. <a class=""low"" href=https://ascyt.com/projects/smsh target=_blank>Learn More</a> | <a class=""low"" href=https://ascyt.com/donate target=_blank>Donate &#x2661;</a></span></div>";

    // minified html
    const string HTML =
        @"<!DOCTYPE html> <html lang=""en""> <head> <meta charset=""UTF-8""> <meta name=""viewport"" content=""width=device-width, initial-scale=0.8""> <link rel=""icon"" href=""{{FAVICON}}"" type=""image/x-icon""> <title>{{TITLE}}</title> <style> html { scroll-behavior: smooth; } body { font-family: '{{FONT}}', Arial, Calibri, 'Trebuchet MS', sans-serif; font-size: large; margin: 10px; text-align: left; background-color: {{#00}}; color: {{#c0}}; line-height: 28px; margin:0; padding:0; box-sizing:border-box; } .section { border-top-style: solid; border-top-width: 3px; border-color: {{#10}}; padding-top: 10px; margin-bottom: 10px; } .high { color: {{#ff}}; } .low { color: {{#80}}; } .card { border-color: {{#80}}; border-radius: 5px; border-width: 2px; border-style: solid; background-color: {{#08}}; padding: 10px; margin-top: 10px; margin-bottom: 10px; } .card.dashed { border-style: dashed; } a { color: #40a6ff; transition: ease-out 0.3s; } a:hover { color: #2f7cbf; transition: ease-in-out 0.2s; } li { margin-left: -10px; } li>ul, ol>li { margin-left: -15px; } table { width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 10px; } th { background-color: {{#20}}; color: {{#ff}}; } th, td { padding: 8px; border: 1px solid {{#40}}; border-style: solid; text-align: center; } tr:nth-child(odd) { background-color: {{#10}}; } .invis>td { border-style: none; } pre, code { white-space: pre-wrap; overflow-x: auto; color: {{#c0}}; background-color: {{#10}}; border-color: {{#80}}; border-width: 1px; border-style: solid; border-right-style: solid; padding: 10px; border-radius: 5px; } #topBtn { margin-right: -100px; position: fixed; bottom: 20px; right: 20px; border: none; outline: none; color: {{#c0}}; background-color: {{#20}}; cursor: pointer; width: 50px; height: 50px; border-radius: 50%; text-align: center; font-size: 18px; line-height: 40px; transition: ease-out 0.3s; } #topBtn:hover { background-color: {{#40}}; transition: ease-out 0.2s; } #credit { font-size: small; line-height: 20px; } code { padding: 2px; padding-left: 3px; padding-right: 3px; } #content { flex: 1; padding: 10px; padding-left: 20px; } #content .sectionDesc { text-align: center; font-size:small; color: {{#80}}; text-align:center; margin-bottom:10px; } #content h1 { color: {{#ff}}; text-align: center; margin-top: 0; margin-bottom: 0px; } #content h2 { color: {{#ff}}; text-decoration: underline; font-size: larger; } #content h3 { color: {{#ff}}; text-decoration: underline; font-size: large; } #content h4 { color: {{#ff}}; text-decoration: underline; font-size: large; font-weight: normal; } #wrapper { display: flex; } #hamburger { position: fixed; font-size: 30px; top:57px; cursor: pointer; vertical-align: middle; z-index: 10; padding: 20px; padding-bottom: 10px; padding-top: 10px; margin-top: 1px; border: none; background-color: {{#20}}; color: {{#ff}}; border-radius: 0 0 10px 0; display: none; } #sidebar { position: fixed; height: 100%; width: 15%; left: 0; top:58px; border-width: 10px; border-right: solid; border-color: {{#0c}}; background-color: {{#08}}; overflow-x: hidden; transition: ease-in-out 0.2s; padding-top: 0; z-index: 9998; } #sidebar .closebtn { background-color: {{#05}}; border-radius: 0 0 10px 0; text-decoration: none; color: {{#ff}}; padding: 20px; padding-bottom: 15px; padding-top: 15px; position: absolute; font-size: 36px; display: none; } #sidebar ul { list-style: none; padding: 0; margin: 0; } #sidebar .sidebar-item a { display:block; text-decoration: none; padding: 10px; text-align: center; } #sidebar a { color: {{#c0}}; transition: ease-out 0.2s; } #sidebar .selected a { font-size: larger; color: {{#ff}}; transition: ease-out 0.3s; } #sidebar .above a { color: {{#80}}; transition: ease-out 0.3s; } #content { transition: margin-left .5s; margin-left: 15%; } #sidebar.mobile .closebtn { display: block; } #content.mobile { margin-left: 0; } #sidebar .sidebar-item:nth-child(even) { background-color: {{#10}}; transition: ease-out 0.15s; } #sidebar .sidebar-item:nth-child(even):hover { background-color: {{#30}}; transition: ease-in 0.1s; } #sidebar .sidebar-item:nth-child(odd) { background-color: {{#20}}; transition: ease-out 0.15s; } #sidebar .sidebar-item:nth-child(odd):hover { background-color: {{#30}}; transition: ease-in 0.1s; } #navbar { position:fixed; top:0; width:100%; border-width: 10px; border-bottom: solid; border-color: {{#0c}}; } #navbar ul { list-style-type: none; margin: 0; padding: 0; margin-left: 15%; overflow: hidden; background-color: {{#08}}; } #navbar ul li { float: left; margin-right: 0px; margin-left: 0px; } #navbar ul li a { display: block; text-align: center; padding: 15px; text-decoration: none; color:{{#ff}}; } #navbar .navbar-item:nth-child(even) { background-color: {{#10}}; transition: ease-out 0.15s; } #navbar .navbar-item:nth-child(even):hover { background-color: {{#30}}; transition: ease-in 0.1s; } #navbar .navbar-item:nth-child(odd) { background-color: {{#20}}; transition: ease-out 0.15s; } #navbar .navbar-item:nth-child(odd):hover { background-color: {{#30}}; transition: ease-in 0.1s; } #navbar .navbar-item-selected { background-color: {{#c0}}; } #navbar .navbar-item-selected a { color: {{#00}}; } #topLeft { float:left; position:fixed; top:0; width:15%; background-color: {{#08}}; } #topLeft p { text-align:center; line-height:1; margin-top: 20px; margin-bottom: 20px; overflow:hidden; text-overflow: ellipsis; white-space: nowrap; color: {{#c0}}; } {{CUSTOM_STYLES}} </style> </head> <body> <div id=""topLeft""> <p>{{TITLE}}</p> </div> <nav id=""navbar""> <ul> {{NAVBAR}} </ul> </nav> <div id=""wrapper""> <button onclick=""openNav()"" id=""hamburger"">#</button> <nav id=""sidebar""> <a href=""javascript:void(0)"" class=""closebtn"" onclick=""closeNav()"">&times;</a> <ul> {{SIDEBAR}} </ul> </nav> <div id=""content""> <div style=""margin-bottom:57px;""></div> <div class=""mobilemargin""></div> {{BODY}} {{CREDIT}} <div class=""mobilemargin""></div> </div> <a onclick=""backToTop()"" id=""topBtn"" title=""{{TO_TOP_TEXT}}""> <svg style=""margin-top:15px;fill:{{#c0}};"" width=""20px"" height=""20px"" version=""1.1"" viewBox=""0 0 100 100"" xmlns=""http://www.w3.org/2000/svg""> <g transform=""translate(0 20.853)""> <rect x=""16.711"" width=""66.578"" height=""10"" ry=""5"" stroke-width="".20617""/> <g transform=""translate(-1.1692 -7.3808)"" stroke-width="".17867""> <rect transform=""rotate(135)"" x=""-18.282"" y=""-64.082"" width=""50"" height=""10"" ry=""5""/> <rect transform=""matrix(.70711 .70711 .70711 -.70711 0 0)"" x=""54.082"" y=""8.2822"" width=""50"" height=""10"" ry=""5""/> </g> </g> </svg> </a> </div> <script> window.mobileCheck = function() { let check = false; (function(a){if(/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i.test(a)||/1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0,4))) check = true;})(navigator.userAgent||navigator.vendor||window.opera); return check; }; window.onscroll = function() {scrollFunction()}; const topBtn = document.getElementById(""topBtn""); function scrollFunction() { if (document.body.scrollTop > 20 || document.documentElement.scrollTop > 20) { topBtn.style.marginRight = ""0""; } else { topBtn.style.marginRight = ""-100px""; } } const sections = document.querySelectorAll('.section'); let observer = new IntersectionObserver(function(entries, self) { entries.forEach(entry => { const id = entry.target.getAttribute('id'); const sidebarItem = document.querySelector(`.sidebar-item[data-link-to=""${id}""]`); if (entry.isIntersecting) { sidebarItem.classList.remove('above'); sidebarItem.classList.add('selected'); } else { sidebarItem.classList.remove('selected'); if(entry.boundingClientRect.top < 0) { sidebarItem.classList.add('above'); } else { sidebarItem.classList.remove('above'); } } }); }, {rootMargin: '0px', threshold: 0}); sections.forEach(section => { observer.observe(section); }); const isMobile = mobileCheck(); const sidebar = document.getElementById('sidebar'); const content = document.getElementById('content'); const hamburger = document.getElementById('hamburger'); const closebtn = document.getElementsByClassName('closebtn')[0]; const mobilemargins = document.getElementsByClassName('mobilemargin'); const topLeft = document.getElementById('topLeft'); const navBar = document.getElementById('navbar'); if (isMobile) { sidebar.classList.add('mobile'); content.classList.add('mobile'); hamburger.classList.add('mobile'); hamburger.style.display = 'block'; for (let mobilemargin of mobilemargins) mobilemargin.style.paddingBottom = '60px'; topLeft.style.display = 'none'; navbar.querySelectorAll('ul').forEach(element => { element.style.marginLeft = 0; }); closeNav(); } function openNav() { sidebar.style.width = '300px'; sidebar.style.borderWidth = '15px'; content.style.marginLeft = '300px'; hamburger.style.display = 'none'; closebtn.style.display = 'block'; } function closeNav() { sidebar.style.width = '0'; sidebar.style.borderWidth = '0'; content.style.marginLeft = '-15px'; hamburger.style.display = 'block'; closebtn.style.display = 'none'; } function sectionClick(formattedName) { if (isMobile) { closeNav(); } } const tabs = document.getElementsByClassName('tab'); const tabSidebars = document.getElementsByClassName('tabSidebar'); const navbarItems = document.getElementsByClassName('navbar-item'); let selectedNavbarItem = undefined; let selectedTab = undefined; const sidebarItems = document.getElementsByClassName('sidebar-item'); function changeTab(formattedName) { if (selectedTab?.id === formattedName) return; for (const sidebarItem of sidebarItems) { sidebarItem.classList.remove('selected'); sidebarItem.classList.remove('above'); } for (let i = 0; i < tabs.length; i++) { if (tabs[i].id == formattedName) { tabs[i].style.display = 'block'; selectedTab = tabs[i]; } else { tabs[i].style.display = 'none'; } } for (let i = 0; i < tabSidebars.length; i++) { if (tabSidebars[i].id == formattedName) { tabSidebars[i].style.display = 'block'; } else { tabSidebars[i].style.display = 'none'; } } for (let i = 0; i < navbarItems.length; i++) { if (selectedNavbarItem) selectedNavbarItem.className = 'navbar-item'; if (navbarItems[i].id == formattedName) { selectedNavbarItem = navbarItems[i]; navbarItems[i].className = 'navbar-item-selected'; break; } } } function findId(items, targetValue, comparisonOperator, reverseComparisonOperator) { let result = targetValue; let hasAnElement = false; for (let i = 0; i < items.length; i++) { if (comparisonOperator(Number(items[i].id), Number(targetValue)) && (!hasAnElement || reverseComparisonOperator(Number(items[i].id), Number(result)))) { result = items[i].id; hasAnElement = true; } } return result; } document.addEventListener('keydown', function(event) { const targetValue = selectedTab.id; let targetId = undefined; if (event.key == 'ArrowLeft') { targetId = findId(tabs, targetValue, function(a, b) {return a < b}, function(a, b) {return a > b}); } else if (event.key == 'ArrowRight') { targetId = findId(tabs, targetValue, function(a, b) {return a > b}, function(a, b) {return a < b}); } else { return; } if (targetId == targetValue || targetId == targetValue) { return; } tabClick(targetId); window.location.hash = targetId; }); function tabClick(formattedName) { changeTab(formattedName); backToTop(); } function findInitialTab(hash) { for (let i = 0; i < tabs.length; i++) { if (tabs[i].id == hash) { return tabs[i]; } const tabsSections = tabs[i].querySelectorAll("".section""); for (let ii = 0; ii < tabsSections.length; ii++) { if (tabsSections[ii].id == hash) { return tabs[i]; } } } return null; } if (tabs.length > 0) { let hash = window.location.hash.slice(1); let initialTab = findInitialTab(hash); if (!initialTab) { hash = '{{INITIAL_HASH}}'; initialTab = findInitialTab(hash); } window.location.hash = hash; if (initialTab) { if (initialTab.id == hash) { tabClick(initialTab.id); } else { changeTab(initialTab.id); } } } function backToTop() { let hash = findInitialTab(window.location.hash.slice(1))?.id; if (!hash) hash = '{{INITIAL_HASH}}'; window.location.hash = hash; window.scrollTo(0, 0); } </script> </body> </html>";
}