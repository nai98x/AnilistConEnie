using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
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
        LogsSettings logsSettings = LogsSettings.FromConfiguration(new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build());

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.WithProcessId()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                theme: SystemConsoleTheme.Colored,
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{ProcessId}] [{Level:u3}]: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                levelSwitch: new LoggingLevelSwitch(LogEventLevel.Information),
                path: "logs/anilistconenie-.log",
                outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm:ss}] [{Level:u3}]: {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: logsSettings.TamanoArchivoBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: logsSettings.ArchivosRetenidos)
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

        // El token de Discord no se versiona: local via User Secrets, en el servidor via variable de entorno (discordToken).
        host.Configuration
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables();

        BotConfiguration botConfig = BotConfiguration.FromConfiguration(host.Configuration);

        // Carpeta con los firebase-*.json: local via User Secrets, en el servidor via variable de entorno (FIREBASE_CREDENTIALS_DIR).
        string firebaseCredentialsDir = host.Configuration.GetValue<string>("FIREBASE_CREDENTIALS_DIR") is { Length: > 0 } dir
            ? dir
            : throw new InvalidOperationException("'FIREBASE_CREDENTIALS_DIR' es obligatorio: configuralo via User Secrets (local) o variable de entorno (servidor)");

        host.Services
            .AddSingleton(botConfig)
            .AddBehaviorSettings(host.Configuration)
            .AddApplication()
            .AddInfrastructure(firebaseCredentialsDir)
            .AddConfiguredDiscordClient(host.Configuration)
            .AddSerilog()
            .AddBotServices();

        return host.Build();
    }
}
