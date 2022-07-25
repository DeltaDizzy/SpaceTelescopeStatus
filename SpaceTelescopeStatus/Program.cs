using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Reflection;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();

    private DiscordSocketClient _client;
    Timer scrapeTimer;
    List<JWSTVisit> webbVisits = new List<JWSTVisit>();
    string dataDirPath = "";
    
    public async Task MainAsync()
    {
        SetupFolders();
        if (new DirectoryInfo(dataDirPath).GetDirectories()[0].GetFiles().Length > 1)
        {
            LoadSchedules();
        }
        else
        {
            StartSearchSite(new object());
        }
        scrapeTimer = new Timer(new TimerCallback(StartSearchSite), new object(), TimeToFirstScrape(), TimeSpan.FromDays(1));

        _client = new DiscordSocketClient();
        _client.Log += Log;
        _client.MessageReceived += HandleTextCommands;
        string token = File.ReadAllText(Path.Combine(dataDirPath, "token.txt"));
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await CreateSlashCommands();
        await Task.Delay(-1);
    }

    private async Task HandleTextCommands(SocketMessage arg)
    {
        // determine if message is a command
        var message = arg as SocketUserMessage;
        if (message == null) return;

        int argPos = 0;
        if (!message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.Author.IsBot)
        {
            return;
        }
        var context = new SocketCommandContext(_client, message);
        if (context.Message.Content.Contains("refresh"))
        {
            StartSearchSite(new object());
            await context.Channel.SendMessageAsync("Refresh Started!");
        }
        else if (context.Message.Content.Contains("now"))
        {
            await context.Channel.SendMessageAsync(embed: BuildJWSTEmbed(GetCurrentVisit()));
        }
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task CreateSlashCommands()
    {
        var webbNow = new SlashCommandBuilder();
        webbNow.WithName("webb-now");
        webbNow.WithDescription("What is the James Webb Space Telescope doing right now?");
        var refreshVisits = new SlashCommandBuilder();
        refreshVisits.WithName("refresh-visits");
        refreshVisits.WithDescription("Refresh visit lists immediately");
        try
        {
            await _client.CreateGlobalApplicationCommandAsync(webbNow.Build());
            await _client.CreateGlobalApplicationCommandAsync(refreshVisits.Build());
        }
        catch (HttpException e)
        {
            var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }

        
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.CommandName)
        {
            case "webb-now":
                await command.RespondAsync(embed: BuildJWSTEmbed(GetCurrentVisit()));
                break;
            case "refresh-visits":
                if (command.User.Id == 317002743803936788) // only I can do this >:)
                {
                    StartSearchSite(new object());
                }
                break;
            default:
                break;
        }
    }

    private Embed BuildJWSTEmbed(JWSTVisit visit)
    {
        long startTimeUnix = new DateTimeOffset(visit.ScheduledStartTime).ToUnixTimeSeconds();
        string startTimeTimestamp = $"<t:{startTimeUnix}:d> <t:{startTimeUnix}:T>";
        EmbedBuilder embed = new EmbedBuilder();
        EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder();
        authorBuilder.Name = "Space Telescope Status";
        authorBuilder.IconUrl = @"https://upload.wikimedia.org/wikipedia/commons/thumb/b/b3/JWST_decal.svg/453px-JWST_decal.svg.png?20100806053442";
        embed.Author = authorBuilder;
        embed.Title = "JWST Live";
        embed.Color = 0xffdd19;
        embed.Description = "What is JWST looing at now?";
        if (visit.TargetName != "N/A")
        {
            embed.AddField("Proposal ID", visit.VisitID[0..4], inline: true);
            embed.AddField("Target", visit.TargetName, inline: true);
            embed.AddField("Instrument+Mode", visit.InstrumentMode, inline: true);
            embed.AddField("Start Time", startTimeTimestamp, inline: true);
            embed.AddField("Duration", visit.Duration, inline: true);
            embed.AddField("Category", visit.Category, inline: true);
            embed.AddField("Keywords", visit.Keywords, inline: true);
        } 
        else
        {
            embed.AddField("JWST is currently in-between targets.", "Please try again later.");
        }
        
        return embed.Build();
    }

    private JWSTVisit GetCurrentVisit()
    {
        var visitsByDateTime = webbVisits.OrderByDescending(k => k.ScheduledStartTime);
        foreach (JWSTVisit visit in visitsByDateTime)
        {
            // if we are AFTER start_time but BEFORE start_time + duration, the visit is right now
            // are we after the start?
            if (visit.ScheduledStartTime < DateTime.Now)
            {
                // are we before the end?
                if (visit.ScheduledStartTime.Add(visit.Duration) > DateTime.Now)
                {
                    //this is the current visit
                    return visit;
                }
            }
        }
        // if there are no matching visits, return an idk visit
        return new JWSTVisit("");
    }

    private void SetupFolders()
    {
        string dllPath = Assembly.GetExecutingAssembly().Location;
        DirectoryInfo dllContainer = new FileInfo(dllPath).Directory;
        DirectoryInfo slnContainer = dllContainer.Parent.Parent.Parent.Parent;

        if (!Directory.Exists($@"{slnContainer}\Data\Schedules"))
        {
            Directory.CreateDirectory($@"{slnContainer}\Data\Schedules");
        }
        dataDirPath = $@"{slnContainer}\Data";
    }

    #region STScI Schedule Scraping
    private void StartSearchSite(object state)
    {
        Task.Run(ScrapeSTScI);
        //ScrapeSTScI();
    }

    private async Task ScrapeSTScI()//private async Task ScrapeSTScI()
    {
        // get schedule list html
        string listUrl = @"https://www.stsci.edu/jwst/science-execution/observing-schedules";
        HttpClient http = new HttpClient();
        var response = await http.GetStringAsync(listUrl);
        //var response = http.GetStringAsync(listUrl);
        //response.Wait();
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(response);
        var containerdiv = doc.DocumentNode.Descendants("div")
            .Where(node => node.GetAttributeValue("id", "") == "tab_8b4634e7-3776-4d67-b685-d266991a2149_0_section").FirstOrDefault();
        var scheduleLinks = containerdiv.Descendants("a");
        List<string> schedules = new List<string>();
        foreach (var link in scheduleLinks)
        {
                schedules.Add(@"https://www.stsci.edu" + link.GetAttributeValue("href", ""));
        }
        //Save schedules as txt files
        foreach (var schedule in schedules)
        {
            response = await http.GetStringAsync(schedule);
            //response.Wait();
            // save to files
            string scheduleName = schedule[^12..];
            string schedulePath = Path.Combine($@"{dataDirPath}\Schedules", scheduleName);
            File.WriteAllText(schedulePath, response);  
        }
        LoadSchedules();

    }

    private async void LoadSchedules()
    {
        //Read schedules into memory
        FileInfo[] scheduleFiles = new DirectoryInfo($"{dataDirPath}\\Schedules").GetFiles();
        await Log(new LogMessage(LogSeverity.Info, "scraper", "file paths loaded"));
        foreach (FileInfo file in scheduleFiles)
        {
            List<string> lines = File.ReadAllLines(file.FullName).ToList();
            await Log(new LogMessage(LogSeverity.Info, "scraper", $"File {file.Name} loaded"));
            lines.RemoveRange(0, 4);
            await Log(new LogMessage(LogSeverity.Info, "scraper", "file header removed"));
            foreach (string line in lines)
            {
                JWSTVisit visit = new JWSTVisit(line);
                await Log(new LogMessage(LogSeverity.Info, "scraper", $"visit {webbVisits.Count + 1} created"));
                webbVisits.Add(visit);
            }
        }
        // remove all non-prime visits
        webbVisits.RemoveAll(j => j.ScheduledStartTime == DateTime.MaxValue);

        Console.WriteLine("done!");
    }

    private TimeSpan TimeToFirstScrape()
    {
        // we scrape every day at 6 am
        TimeSpan day = new TimeSpan(24, 0, 0);
        TimeSpan now = TimeSpan.Parse(DateTime.Now.ToString("HH:mm"));
        TimeSpan scrapeTime = new TimeSpan(6, 0, 0);
        TimeSpan timeUntilScrape = (day - now) + scrapeTime;
        return timeUntilScrape;
    }
    #endregion
}