using Microsoft.Extensions.DependencyInjection;
using Sherlock.Net.Cli.Commands;
using Sherlock.Net.Cli.Infrastructure;
using Sherlock.Net.Cli.Rendering;
using Sherlock.Net.Core;
using Spectre.Console.Cli;

var services = new ServiceCollection();

services.AddSherlock();
services.AddSingleton<IResultRenderer, SpectreResultRenderer>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp<SearchCommand>(registrar);

app.Configure(config =>
{
    config.SetApplicationName("sherlock");
    config.SetApplicationVersion(SherlockDefaults.Version);
});

return await app.RunAsync(args);
