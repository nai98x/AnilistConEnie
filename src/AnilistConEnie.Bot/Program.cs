using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Helpers;
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

        BotConfiguration botConfig = BotConfiguration.FromConfiguration(host.Configuration);

        try
        {
            host.Services
                .AddSingleton(botConfig)
                .AddInfrastructure()
                .AddConfiguredDiscordClient()
                .AddLogging(builder => builder.AddConsole())
                .AddSingleton<BotStateService>()
                .AddSingleton<DiscordHelper>()
                .AddSingleton<DiscordBotService>()
                .AddHostedService(sp => sp.GetRequiredService<DiscordBotService>());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }

        return host.Build();
    }
}
