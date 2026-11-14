using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Configuration;

public record AntiSpamSettings
{
    public required int CanalesDistintos { get; init; }
    public required double VentanaMinutos { get; init; }

    public static AntiSpamSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("AntiSpam");
        return new AntiSpamSettings
        {
            CanalesDistintos = section.RequireInt("CanalesDistintos"),
            VentanaMinutos = section.RequireDouble("VentanaMinutos"),
        };
    }
}

public record LimpiezaMiembrosSettings
{
    public required int InactividadMeses { get; init; }
    public required int RolInactivoMeses { get; init; }
    public required long XpMinimoInactivo { get; init; }
    public required int GraciaNoVinculadoDias { get; init; }
    public required int CuentaNuevaMeses { get; init; }

    public static LimpiezaMiembrosSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("LimpiezaMiembros");
        return new LimpiezaMiembrosSettings
        {
            InactividadMeses = section.RequireInt("InactividadMeses"),
            RolInactivoMeses = section.RequireInt("RolInactivoMeses"),
            XpMinimoInactivo = section.RequireLong("XpMinimoInactivo"),
            GraciaNoVinculadoDias = section.RequireInt("GraciaNoVinculadoDias"),
            CuentaNuevaMeses = section.RequireInt("CuentaNuevaMeses"),
        };
    }
}

public record CooldownsSettings
{
    public required int InvitePermisoMinutos { get; init; }
    public required int TeiouApodoHoras { get; init; }

    public static CooldownsSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Cooldowns");
        return new CooldownsSettings
        {
            InvitePermisoMinutos = section.RequireInt("InvitePermisoMinutos"),
            TeiouApodoHoras = section.RequireInt("TeiouApodoHoras"),
        };
    }
}

public record XpSettings
{
    public required int MinPorMensaje { get; init; }
    public required int MaxPorMensaje { get; init; }
    public required int MinBooster { get; init; }
    public required int MaxBooster { get; init; }
    public required int CooldownSegundos { get; init; }

    public static XpSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Xp");
        return new XpSettings
        {
            MinPorMensaje = section.RequireInt("MinPorMensaje"),
            MaxPorMensaje = section.RequireInt("MaxPorMensaje"),
            MinBooster = section.RequireInt("MinBooster"),
            MaxBooster = section.RequireInt("MaxBooster"),
            CooldownSegundos = section.RequireInt("CooldownSegundos"),
        };
    }
}

public record MensajesDiariosSettings
{
    public required int DiasRetencion { get; init; }
    public required int RolActivoVentanaDias { get; init; }
    public required int RolActivoMasDeMensajes { get; init; }
    public required int RolActivoMinimoFueraDelCanalPrincipal { get; init; }
    public required int RolActivoMinimoDias { get; init; }
    public required int RolActivoAntiguedadMinimaMeses { get; init; }

    public static MensajesDiariosSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("MensajesDiarios");
        MensajesDiariosSettings settings = new()
        {
            DiasRetencion = section.RequireInt("DiasRetencion"),
            RolActivoVentanaDias = section.RequireInt("RolActivoVentanaDias"),
            RolActivoMasDeMensajes = section.RequireInt("RolActivoMasDeMensajes"),
            RolActivoMinimoFueraDelCanalPrincipal = section.RequireInt("RolActivoMinimoFueraDelCanalPrincipal"),
            RolActivoMinimoDias = section.RequireInt("RolActivoMinimoDias"),
            RolActivoAntiguedadMinimaMeses = section.RequireInt("RolActivoAntiguedadMinimaMeses"),
        };

        if (settings.RolActivoVentanaDias > settings.DiasRetencion)
            throw new InvalidOperationException($"{section.Path}:RolActivoVentanaDias no puede superar a DiasRetencion: la ventana del rol se calcula sobre los días retenidos");

        return settings;
    }
}

public record ConfesionesSettings
{
    public required int PorcentajePorReaccion { get; init; }

    public static ConfesionesSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Confesiones");
        return new ConfesionesSettings
        {
            PorcentajePorReaccion = section.RequireInt("PorcentajePorReaccion"),
        };
    }
}

public record SubirImagenSettings
{
    public required long MaxTamanoBytes { get; init; }
    public required int CooldownMinutos { get; init; }

    public static SubirImagenSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("SubirImagen");
        return new SubirImagenSettings
        {
            MaxTamanoBytes = section.RequireLong("MaxTamanoBytes"),
            CooldownMinutos = section.RequireInt("CooldownMinutos"),
        };
    }
}

public record TraduccionSettings
{
    public required string Url { get; init; }

    public static TraduccionSettings FromConfiguration(IConfiguration configuration)
    {
        string? url = configuration["Traduccion:Url"];
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Configuración faltante o inválida en appsettings.json: Traduccion:Url");
        return new TraduccionSettings { Url = url };
    }
}

public record LogsSettings
{
    public required long TamanoArchivoBytes { get; init; }
    public required int ArchivosRetenidos { get; init; }

    public static LogsSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Logs");
        return new LogsSettings
        {
            TamanoArchivoBytes = section.RequireLong("TamanoArchivoBytes"),
            ArchivosRetenidos = section.RequireInt("ArchivosRetenidos"),
        };
    }
}

// La ruta no va en appsettings.json (repo público): se pasa por variable de entorno
// Backups__RutaEstado, igual que la connection string.
public record BackupsSettings
{
    public required string RutaEstado { get; init; }

    public static BackupsSettings FromConfiguration(IConfiguration configuration)
    {
        string? ruta = configuration["Backups:RutaEstado"];
        if (string.IsNullOrWhiteSpace(ruta))
            throw new InvalidOperationException("'Backups:RutaEstado' es obligatoria en release: configurala via variable de entorno Backups__RutaEstado");
        return new BackupsSettings { RutaEstado = ruta };
    }
}

// La redirect URI no va en appsettings.json (repo público): se pasa por variable de entorno
// VinculacionAnilist__RedirectUri, igual que el client secret.
public record VinculacionAnilistSettings
{
    // Discord deja editar la respuesta de una interacción solo durante 15 minutos.
    private const int ValidezMaximaMinutos = 14;

    public required int ClientId { get; init; }
    public required Uri RedirectUri { get; init; }
    public required int PuertoLocal { get; init; }
    public required int ValidezEnlaceMinutos { get; init; }

    public static VinculacionAnilistSettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("VinculacionAnilist");

        if (!Uri.TryCreate(section["RedirectUri"], UriKind.Absolute, out Uri? redirectUri) || (redirectUri.Scheme != Uri.UriSchemeHttps && !redirectUri.IsLoopback))
            throw new InvalidOperationException("'VinculacionAnilist:RedirectUri' es obligatoria y debe ser una URL https (o http en localhost): configurala via User Secrets (local) o variable de entorno VinculacionAnilist__RedirectUri (servidor)");

        VinculacionAnilistSettings settings = new()
        {
            ClientId = section.RequireInt("ClientId"),
            RedirectUri = redirectUri,
            PuertoLocal = section.RequireInt("PuertoLocal"),
            ValidezEnlaceMinutos = section.RequireInt("ValidezEnlaceMinutos"),
        };

        if (settings.ClientId <= 0)
            throw new InvalidOperationException($"{section.Path}:ClientId debe ser el id del cliente de AniList configurado con la redirect URI");

        if (settings.ValidezEnlaceMinutos is < 1 or > ValidezMaximaMinutos)
            throw new InvalidOperationException($"{section.Path}:ValidezEnlaceMinutos debe estar entre 1 y {ValidezMaximaMinutos}");

        return settings;
    }
}

/// <summary>Gateway health watchdog.</summary>
/// <param name="WatchdogSilenceTimeout">
/// Time without receiving any gateway event after which the process is stopped to be restarted.
/// </param>
public record GatewaySettings(TimeSpan WatchdogCheckInterval, TimeSpan WatchdogSilenceTimeout)
{
    public static GatewaySettings FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Gateway");
        return new GatewaySettings(
            TimeSpan.FromSeconds(section.GetValue<int?>("WatchdogCheckIntervaloSegundos") ?? 30),
            TimeSpan.FromSeconds(section.GetValue<int?>("WatchdogSilencioTimeoutSegundos") ?? 300));
    }
}

internal static class SettingsBindingExtensions
{
    public static int RequireInt(this IConfigurationSection section, string key)
    {
        string? raw = section[key];
        if (string.IsNullOrEmpty(raw) || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            throw new InvalidOperationException($"Configuración faltante o inválida en appsettings.json: {section.Path}:{key}");
        return value;
    }

    public static long RequireLong(this IConfigurationSection section, string key)
    {
        string? raw = section[key];
        if (string.IsNullOrEmpty(raw) || !long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            throw new InvalidOperationException($"Configuración faltante o inválida en appsettings.json: {section.Path}:{key}");
        return value;
    }

    public static double RequireDouble(this IConfigurationSection section, string key)
    {
        string? raw = section[key];
        if (string.IsNullOrEmpty(raw) || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            throw new InvalidOperationException($"Configuración faltante o inválida en appsettings.json: {section.Path}:{key}");
        return value;
    }
}

public static class BehaviorSettingsExtensions
{
    public static IServiceCollection AddBehaviorSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSingleton(AntiSpamSettings.FromConfiguration(configuration))
            .AddSingleton(LimpiezaMiembrosSettings.FromConfiguration(configuration))
            .AddSingleton(CooldownsSettings.FromConfiguration(configuration))
            .AddSingleton(XpSettings.FromConfiguration(configuration))
            .AddSingleton(MensajesDiariosSettings.FromConfiguration(configuration))
            .AddSingleton(ConfesionesSettings.FromConfiguration(configuration))
            .AddSingleton(SubirImagenSettings.FromConfiguration(configuration))
            .AddSingleton(TraduccionSettings.FromConfiguration(configuration))
            .AddSingleton(GatewaySettings.FromConfiguration(configuration))
            .AddSingleton(VinculacionAnilistSettings.FromConfiguration(configuration));

#if !DEBUG
        // El backup solo existe en el bot desplegado; en debug no se exige la ruta.
        services.AddSingleton(BackupsSettings.FromConfiguration(configuration));
#endif

        return services;
    }
}
