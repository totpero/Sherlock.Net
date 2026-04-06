using System.ComponentModel;
using Spectre.Console.Cli;

namespace Sherlock.Net.Cli.Commands;

public sealed class SearchCommandSettings : CommandSettings
{
    [CommandArgument(0, "<USERNAMES>")]
    [Description("One or more usernames to search for")]
    public string[] Usernames { get; set; } = [];

    [CommandOption("--timeout <SECONDS>")]
    [Description("Time in seconds to wait for response (default: 60)")]
    [DefaultValue(60)]
    public int Timeout { get; set; } = 60;

    [CommandOption("--proxy <URL>")]
    [Description("Proxy URL (e.g., socks5://127.0.0.1:9050 for Tor)")]
    public string? Proxy { get; set; }

    [CommandOption("--site <NAME>")]
    [Description("Limit search to specific site(s)")]
    public string[]? Site { get; set; }

    [CommandOption("--json <PATH_OR_URL>")]
    [Description("Custom data.json file path or URL")]
    public string? DataSource { get; set; }

    [CommandOption("--csv")]
    [Description("Export results as CSV")]
    public bool Csv { get; set; }

    [CommandOption("--txt")]
    [Description("Export results as TXT")]
    public bool Txt { get; set; }

    [CommandOption("--json-export")]
    [Description("Export results as JSON")]
    public bool JsonExport { get; set; }

    [CommandOption("-o|--output <DIR>")]
    [Description("Output directory for export files")]
    public string? OutputDir { get; set; }

    [CommandOption("--print-all")]
    [Description("Show all results, not just found accounts")]
    public bool PrintAll { get; set; }

    [CommandOption("--nsfw")]
    [Description("Include NSFW sites in search")]
    public bool Nsfw { get; set; }

    [CommandOption("-b|--browse")]
    [Description("Open found URLs in default browser")]
    public bool Browse { get; set; }

    [CommandOption("--no-color")]
    [Description("Disable colored output")]
    public bool NoColor { get; set; }

    [CommandOption("--concurrency <COUNT>")]
    [Description("Maximum concurrent requests (default: 20)")]
    [DefaultValue(20)]
    public int Concurrency { get; set; } = 20;
}
