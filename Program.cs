using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UtilityCli;

class Program
{
    static void Main(string[] args)
    {
        var url = "https://www.eastrailflatswoodinville.com";

        var cli = CliArgs.Parse(args);
        bool save = cli.GetBoolean("save") ?? false;
        string baselineFile = cli.GetString() ?? "baseline.md";

        string actions = ExtractPageActions(url);

        if (File.Exists(baselineFile))
        {
            string baseline = File.ReadAllText(baselineFile);

            if (baseline != actions)
            {
                Console.WriteLine("DIFFERENCE FOUND!");
                Console.WriteLine();
                Console.WriteLine(actions);
                Console.WriteLine();

                if (!save)
                {
                    Console.WriteLine($"Run again using `-s|--save` to update {baselineFile} with the new results.");
                }
            }
            else
            {
                Console.WriteLine("NO DIFFERENCES FROM BASELINE");
                Console.WriteLine();
                Console.WriteLine(actions);
            }
        }

        if (save)
        {
            File.WriteAllText(baselineFile, actions);
            Console.WriteLine($"Baseline file {baselineFile} updated with results.");
        }
    }

    static string ExtractPageActions(string url)
    {
        var web = new HtmlWeb();
        var doc = web.Load(url);
        var actions = new StringBuilder();

        // Extract all links
        var links = doc.DocumentNode.SelectNodes("//a[@href]");

        if (links.Any())
        {
            actions.AppendLine("## LINKS");
            actions.AppendLine();

            foreach (var link in links)
            {
                string linkText = link.InnerText;

                if (string.IsNullOrWhiteSpace(linkText))
                {
                    var img = link.SelectNodes("img[@alt]").FirstOrDefault();
                    if (img is not null)
                    {
                        linkText = img.Attributes["alt"].Value;
                    }
                }

                actions.AppendLine($"- {linkText}:\n  - {link.Attributes["href"].Value}");
            }
        }

        // Extract all buttons
        var buttons = doc.DocumentNode.SelectNodes("//button");

        if (buttons.Any())
        {
            actions.AppendLine();
            actions.AppendLine("## BUTTONS");
            actions.AppendLine();

            foreach (var button in buttons)
            {
                actions.AppendLine($"- {button.InnerText}");
            }
        }

        return actions.ToString();
    }
}
