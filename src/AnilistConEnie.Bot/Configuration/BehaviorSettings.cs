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
            .AddSingleton(ConfesionesSettings.FromConfiguration(configuration))
            .AddSingleton(SubirImagenSettings.FromConfiguration(configuration));

#if !DEBUG
        // El backup solo existe en el bot desplegado; en debug no se exige la ruta.
        services.AddSingleton(BackupsSettings.FromConfiguration(configuration));
#endif

        return services;
    }
}
