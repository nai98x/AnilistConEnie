using AnilistConEnie.Extensions;
using Microsoft.Extensions.DependencyInjection;
using AnilistConEnie.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie;

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

        host.Services
            .AddConfiguredDiscordClient()
            .AddLogging(builder => builder
                .AddConsole())
            .AddSingleton<MainService>()
            .AddSingleton<Events>()
            .AddHostedService<DiscordBotService>();
        
        return host.Build();
    }
}
