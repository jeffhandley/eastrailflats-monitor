using System;
using System.IO;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using UtilityCli;

class Program
{
    const string EastrailFlatsUrl = "https://www.eastrailflatswoodinville.com";

    static void Main(string[] args)
    {
        var cli = CliArgs.Parse(args);
        bool save = cli.GetBoolean("save") ?? false;

        string? twilioAccountSid = cli.GetString("twilio-account-sid");
        string? twilioAuthToken = cli.GetString("twilio-auth-token");
        string? notificationTo = cli.GetString("notification-to", ["to"]);
        string? notificationFrom = cli.GetString("notification-from", ["from"]);

        string baselineFile = cli.GetString() ?? "baseline.md";

        string actions = ExtractPageActions(EastrailFlatsUrl).ReplaceLineEndings("\n");
        Console.WriteLine(actions);

        bool hasDifferences = HasDifferences(baselineFile, actions);

        if (hasDifferences)
        {
            Console.WriteLine();
            Console.WriteLine("DIFFERENCE FOUND!");

            if (!save)
            {
                Console.WriteLine($"Run again using `-s|--save` to update {baselineFile} with the new results.");
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("NO DIFFERENCES FROM BASELINE");
        }

        if (twilioAccountSid is not null && twilioAuthToken is not null && notificationTo is not null && notificationFrom is not null)
        {
            SendNotification(hasDifferences, twilioAccountSid, twilioAuthToken, notificationTo, notificationFrom);
        }

        if (save)
        {
            File.WriteAllText(baselineFile, actions);
            Console.WriteLine($"Baseline file {baselineFile} updated with results.");
        }
    }

    static bool HasDifferences(string baselineFile, string actions)
    {
        if (File.Exists(baselineFile))
        {
            string baseline = File.ReadAllText(baselineFile).ReplaceLineEndings("\n");
            return baseline != actions;
        }

        return true;
    }

    static void SendNotification(bool hasDifferences, string accountSid, string authToken, string notificationTo, string notificationFrom)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        DateTime currentTime = TimeZoneInfo.ConvertTime(DateTime.Now, timeZone);
        string formattedTime = currentTime.ToString("MMMM d");

        TwilioClient.Init(accountSid, authToken);

        string body = $"{formattedTime}\n{(hasDifferences ? $"CHANGES FOUND!\n{EastrailFlatsUrl}" : "No changes found.")}";

        var message = MessageResource.Create(
            to: new PhoneNumber(notificationTo),
            from: new PhoneNumber(notificationFrom),
            body: body
        );

        Console.WriteLine($"Twilio Message sent: {message.Sid}");
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
