using Microsoft.Extensions.DependencyInjection;
using Sherlock.Net.Cli.Commands;
using Sherlock.Net.Cli.Infrastructure;
using Sherlock.Net.Cli.Rendering;
using Sherlock.Net.Core.Services;
using Sherlock.Net.Core.Services.Exporters;
using Spectre.Console.Cli;

var services = new ServiceCollection();

services.AddHttpClient("sherlock", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
});

services.AddSingleton<ISiteDataProvider, SiteDataProvider>();
services.AddSingleton<IWafDetector, WafDetector>();
services.AddSingleton<ISiteChecker, SiteChecker>();
services.AddSingleton<ISherlockService, SherlockService>();
services.AddSingleton<IResultRenderer, SpectreResultRenderer>();
services.AddSingleton<IResultExporter, TxtResultExporter>();
services.AddSingleton<IResultExporter, CsvResultExporter>();
services.AddSingleton<IResultExporter, JsonResultExporter>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp<SearchCommand>(registrar);

app.Configure(config =>
{
    config.SetApplicationName("sherlock");
    config.SetApplicationVersion("1.0.0");
});

return await app.RunAsync(args);
