using Sherlock.Net.Core.Models;
using Spectre.Console;

namespace Sherlock.Net.Cli.Rendering;

public sealed class SpectreResultRenderer : IResultRenderer
{
    public void RenderBanner()
    {
        AnsiConsole.MarkupLine("[bold cyan]  ____  _               _            _      _   _      _   [/]");
        AnsiConsole.MarkupLine(@"[bold cyan] / ___|| |__   ___ _ __| | ___   ___| | __ | \ | | ___| |_ [/]");
        AnsiConsole.MarkupLine(@"[bold cyan] \___ \| '_ \ / _ \ '__| |/ _ \ / __| |/ / |  \| |/ _ \ __|[/]");
        AnsiConsole.MarkupLine(@"[bold cyan]  ___) | | | |  __/ |  | | (_) | (__|   < _| |\  |  __/ |_ [/]");
        AnsiConsole.MarkupLine(@"[bold cyan] |____/|_| |_|\___|_|  |_|\___/ \___|_|\_(_)_| \_|\___|\__|[/]");
        AnsiConsole.WriteLine();
    }

    public void RenderSearchStart(string username, int siteCount)
    {
        AnsiConsole.MarkupLine($"[yellow][[*]] Checking username [bold]{Markup.Escape(username)}[/] on {siteCount} sites[/]");
    }

    public void RenderResult(QueryResult result, bool printAll)
    {
        switch (result.Status)
        {
            case QueryStatus.Claimed:
                AnsiConsole.MarkupLine($"[green][[+]] {Markup.Escape(result.SiteName)}: {Markup.Escape(result.ProfileUrl)}[/]");
                break;

            case QueryStatus.Available when printAll:
                AnsiConsole.MarkupLine($"[dim][[−]] {Markup.Escape(result.SiteName)}: Not Found![/]");
                break;

            case QueryStatus.Illegal when printAll:
                AnsiConsole.MarkupLine($"[dim][[−]] {Markup.Escape(result.SiteName)}: Illegal Username Format[/]");
                break;

            case QueryStatus.Waf when printAll:
                AnsiConsole.MarkupLine($"[red][[−]] {Markup.Escape(result.SiteName)}: Blocked by bot detection (proxy may help)[/]");
                break;

            case QueryStatus.Unknown when printAll:
                var ctx = result.Context ?? "Unknown error";
                AnsiConsole.MarkupLine($"[red][[−]] {Markup.Escape(result.SiteName)}: {Markup.Escape(ctx)}[/]");
                break;
        }
    }

    public void RenderSearchComplete(string username, int claimedCount)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow][[*]] Search completed: [bold]{claimedCount}[/] account(s) found for [bold]{Markup.Escape(username)}[/][/]");
        AnsiConsole.WriteLine();
    }
}
