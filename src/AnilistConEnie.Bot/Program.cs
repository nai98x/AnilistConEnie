using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot;

public static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Iniciando servicio principal AnilistConEnie");
        IHost host = CreateHostBuilder(args);
        Console.WriteLine("Servicio principal iniciado correctamente");

        await host.RunAsync();
    }

    private static IHost CreateHostBuilder(string[] args)
    {
        HostApplicationBuilder host = Host.CreateApplicationBuilder(args);

        try
        {
            host.Services
                .AddInfrastructure()
                .AddConfiguredDiscordClient()
                .AddLogging(builder => builder
                    .AddConsole())
                .AddSingleton<MainService>()
                .AddSingleton<SingletonService>()
                .AddSingleton<Events>()
                .AddHostedService<DiscordBotService>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }

        return host.Build();
    }
}
