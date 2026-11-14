using System.Net;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Infrastructure.Anilist;
using AnilistConEnie.Infrastructure.Extensions;
using DSharpPlus.Net.Gateway;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            .Filter.ByExcluding(EsCierreDelGatewayPedido)
            .WriteTo.Console(
                theme: SystemConsoleTheme.Colored,
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{ProcessId}] [{Level:u3}]: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                levelSwitch: new LoggingLevelSwitch(LogEventLevel.Information),
                path: "logs/anilistconenie-.log",
                outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm:ss}] [{Level:u3}]: {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: logsSettings.TamanoArchivoBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: logsSettings.ArchivosRetenidos)
            .WriteToOtlpSiHabilitado()
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

    // DSharpPlus loguea como Error el fin del gateway aunque lo haya pedido el propio bot al apagarse (cada deploy).
    private static bool EsCierreDelGatewayPedido(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue? fuente)
        && fuente is ScalarValue { Value: "DSharpPlus.Clients.IShardOrchestrator" }
        && logEvent.Properties.TryGetValue("reason", out LogEventPropertyValue? motivo)
        && motivo is ScalarValue { Value: GatewayDisconnectReason.UserRequested };

    private static IHost CreateHostBuilder(string[] args)
    {
        WebApplicationBuilder host = WebApplication.CreateSlimBuilder(args);

        // El token de Discord no se versiona: local via User Secrets, en el servidor via variable de entorno (discordToken).
        host.Configuration
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables();

        BotConfiguration botConfig = BotConfiguration.FromConfiguration(host.Configuration);

        // Carpeta con los firebase-*.json: local via User Secrets, en el servidor via variable de entorno (FIREBASE_CREDENTIALS_DIR).
        string firebaseCredentialsDir = host.Configuration.GetValue<string>("FIREBASE_CREDENTIALS_DIR") is { Length: > 0 } dir
            ? dir
            : throw new InvalidOperationException("'FIREBASE_CREDENTIALS_DIR' es obligatorio: configuralo via User Secrets (local) o variable de entorno (servidor)");

        // Connection string de la base de datos: local via User Secrets (apuntando al túnel SSH), en el servidor via variable de entorno (ConnectionStrings__Database).
        string dbConnectionString = host.Configuration.GetConnectionString("Database") is { Length: > 0 } db
            ? db
            : throw new InvalidOperationException("'ConnectionStrings:Database' es obligatoria: configurala via User Secrets (local) o variable de entorno (servidor)");

        // Connection string de la base de Yumiko (otra base, con su propio rol): ahí se espeja el vínculo de AniList.
        string yumikoConnectionString = host.Configuration.GetConnectionString("Yumiko") is { Length: > 0 } yumiko
            ? yumiko
            : throw new InvalidOperationException("'ConnectionStrings:Yumiko' es obligatoria: configurala via User Secrets (local) o variable de entorno (servidor)");

        // Callback de la vinculación de AniList: Kestrel escucha solo en loopback y Tailscale Funnel lo publica.
        VinculacionAnilistSettings vinculacion = VinculacionAnilistSettings.FromConfiguration(host.Configuration);
        string anilistClientSecret = host.Configuration.GetValue<string>("VinculacionAnilist:ClientSecret") is { Length: > 0 } secret
            ? secret
            : throw new InvalidOperationException("'VinculacionAnilist:ClientSecret' es obligatorio: configuralo via User Secrets (local) o variable de entorno VinculacionAnilist__ClientSecret (servidor)");
        AnilistOAuthCredenciales anilistOAuth = new(vinculacion.ClientId, anilistClientSecret, vinculacion.RedirectUri.OriginalString);

        host.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.AddServerHeader = false;
            kestrel.Listen(IPAddress.Loopback, vinculacion.PuertoLocal);
        });

        host.Services
            .AddSingleton(botConfig)
            .AddBehaviorSettings(host.Configuration)
            .AddApplication()
            .AddInfrastructure(firebaseCredentialsDir, dbConnectionString, yumikoConnectionString, anilistOAuth)
            .AddConfiguredDiscordClient(host.Configuration)
            .AddSerilog()
            .AddObservabilidad()
            .AddBotServices();

        WebApplication app = host.Build();
        app.MapVinculacionAnilist();
        return app;
    }
}
