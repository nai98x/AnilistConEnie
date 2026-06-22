using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace AnilistConEnie.Bot;

public static class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithProcessId()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                theme: SystemConsoleTheme.Colored,
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{ProcessId}] [{Level:u3}]: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                levelSwitch: new LoggingLevelSwitch(LogEventLevel.Information),
                path: "logs/anilistconenie-.log",
                outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm:ss}] [{Level:u3}]: {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 8_388_608, /* 8 megabytes */
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 50)
            .CreateLogger();

        try
        {
            Log.Information("Iniciando servicio principal AnilistConEnie");
            IHost host = CreateHostBuilder(args);
            Log.Information("Servicio principal iniciado correctamente");

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "El servicio principal terminó de forma inesperada");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHost CreateHostBuilder(string[] args)
    {
        HostApplicationBuilder host = Host.CreateApplicationBuilder(args);

        BotConfiguration botConfig = BotConfiguration.FromConfiguration(host.Configuration);

        host.Services
            .AddSingleton(botConfig)
            .AddInfrastructure()
            .AddConfiguredDiscordClient()
            .AddSerilog()
            .AddBotServices();

        return host.Build();
    }
}
