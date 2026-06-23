using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace AnilistConEnie.Bot.Configuration;

public class BotConfiguration
{
    public required ulong GuildId { get; init; }
    public required ulong OwnerId { get; init; }
    public required bool LoadXpUserHistoryOnDebug { get; init; }
    public required ChannelConfiguration Channels { get; init; }
    public required RolesConfiguration Roles { get; init; }
    public required EmotesConfiguration Emotes { get; init; }
    public required IReadOnlyList<PaisTimezoneConfiguration> PaisTimezones { get; init; }
    public required IReadOnlyList<FechaEntradaExcepcion> FechasEntradaExcepciones { get; init; }

    public DateTimeOffset GetFechaEntrada(ulong userId, DateTimeOffset joinedAt)
        => FechasEntradaExcepciones.FirstOrDefault(x => x.UserId == userId)?.FechaEntrada ?? joinedAt;

    public class ChannelConfiguration
    {
        public required ulong General { get; init; }
        public required ulong ConfigBots { get; init; }
        public required ulong Tsuma { get; init; }
        public required ulong Mudae { get; init; }
        public required ulong ComandosCanal { get; init; }
        public required ulong ComandosForo { get; init; }
        public required ulong Sugerencias { get; init; }
        public required ulong LogChannelInfo { get; init; }
        public required ulong LogChannelError { get; init; }
        public required ulong LogChannelPuerta { get; init; }
        public required ulong Perfiles { get; init; }
        public required ulong Playroom { get; init; }
        public required ulong Moderacion { get; init; }
        public required IntercambiosChannelConfiguration Intercambios { get; init; }

        public class IntercambiosChannelConfiguration
        {
            public required ulong Reviews { get; init; }
            public required ulong Anime { get; init; }
            public required ulong Manga { get; init; }
            public required ulong Pelis { get; init; }
            public required ulong Series { get; init; }
            public required ulong Musica { get; init; }
            public required ulong Fanarts { get; init; }
        }
    }

    public class RolesConfiguration
    {
        public required ulong Miembro { get; init; }
        public required ulong Fundador { get; init; }
        public required ulong NoVinculado { get; init; }
        public required ulong Inactivo { get; init; }
        public required ulong Invite { get; init; }
        public required ulong KamiSama { get; init; }
        public required ulong Colaborador { get; init; }
        public required ulong Cumple { get; init; }
        public required ulong GeneroMasculino { get; init; }
        public required ulong GeneroFemenino { get; init; }
        public required RangosConfiguration Rangos { get; init; }
        public required IReadOnlyList<ColorRangoConfiguration> ColoresRango { get; init; }

        public class RangosConfiguration
        {
            public required ulong Tama { get; init; }
            public required ulong Casual { get; init; }
            public required ulong Kouhai { get; init; }
            public required ulong Senpai { get; init; }
            public required ulong Hikikomori { get; init; }
            public required ulong Sensei { get; init; }
            public required ulong Ousama { get; init; }
            public required ulong Teiou { get; init; }
        }
    }

    public class EmotesConfiguration
    {
        public required EmoteIds UmaPoints { get; init; }
        public required EmoteIds Worrysad { get; init; }
        public required ulong ConfessionReaction { get; init; }

        public record EmoteIds(ulong Prod, ulong Test)
        {
            public ulong Get(bool isDebug) => isDebug ? Test : Prod;
        }
    }

    public record FechaEntradaExcepcion(string Nombre, ulong UserId, DateTimeOffset FechaEntrada);
    public record PaisTimezoneConfiguration(ulong RoleId, string Timezone);
    public record ColorRangoConfiguration(ulong RoleId, string Nombre, string Rango);

    public static BotConfiguration FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection ids = configuration.GetSection("Ids");
        IConfigurationSection channels = ids.GetSection("Channels");
        IConfigurationSection intercambios = channels.GetSection("Intercambios");
        IConfigurationSection roles = ids.GetSection("Roles");
        IConfigurationSection rangos = roles.GetSection("Rangos");
        IConfigurationSection emotes = ids.GetSection("Emotes");

        return new BotConfiguration
        {
            GuildId = RequireUlong(ids, "GuildId"),
            OwnerId = RequireUlong(ids, "OwnerId"),
            LoadXpUserHistoryOnDebug = bool.TryParse(configuration["LoadXpUserHistoryOnDebug"], out bool loadXpUserHistoryOnDebug) && loadXpUserHistoryOnDebug,
            Channels = new ChannelConfiguration
            {
                General = RequireUlong(channels, "General"),
                ConfigBots = RequireUlong(channels, "ConfigBots"),
                Tsuma = RequireUlong(channels, "Tsuma"),
                Mudae = RequireUlong(channels, "Mudae"),
                ComandosCanal = RequireUlong(channels, "ComandosCanal"),
                ComandosForo = RequireUlong(channels, "ComandosForo"),
                Sugerencias = RequireUlong(channels, "Sugerencias"),
                LogChannelInfo = RequireUlong(channels, "LogChannelInfo"),
                LogChannelError = RequireUlong(channels, "LogChannelError"),
                Perfiles = RequireUlong(channels, "Perfiles"),
                Playroom = RequireUlong(channels, "Playroom"),
                Moderacion = RequireUlong(channels, "Moderacion"),
                LogChannelPuerta = RequireUlong(channels, "LogChannelPuerta"),
                Intercambios = new ChannelConfiguration.IntercambiosChannelConfiguration
                {
                    Reviews = RequireUlong(intercambios, "Reviews"),
                    Anime = RequireUlong(intercambios, "Anime"),
                    Manga = RequireUlong(intercambios, "Manga"),
                    Pelis = RequireUlong(intercambios, "Pelis"),
                    Series = RequireUlong(intercambios, "Series"),
                    Musica = RequireUlong(intercambios, "Musica"),
                    Fanarts = RequireUlong(intercambios, "Fanarts"),
                }
            },
            Roles = new RolesConfiguration
            {
                Miembro = RequireUlong(roles, "Miembro"),
                Fundador = RequireUlong(roles, "Fundador"),
                NoVinculado = RequireUlong(roles, "NoVinculado"),
                Inactivo = RequireUlong(roles, "Inactivo"),
                Invite = RequireUlong(roles, "Invite"),
                KamiSama = RequireUlong(roles, "KamiSama"),
                Colaborador = RequireUlong(roles, "Colaborador"),
                Cumple = RequireUlong(roles, "Cumple"),
                GeneroMasculino = RequireUlong(roles, "GeneroMasculino"),
                GeneroFemenino = RequireUlong(roles, "GeneroFemenino"),
                Rangos = new RolesConfiguration.RangosConfiguration
                {
                    Tama = RequireUlong(rangos, "Tama"),
                    Casual = RequireUlong(rangos, "Casual"),
                    Kouhai = RequireUlong(rangos, "Kouhai"),
                    Senpai = RequireUlong(rangos, "Senpai"),
                    Hikikomori = RequireUlong(rangos, "Hikikomori"),
                    Sensei = RequireUlong(rangos, "Sensei"),
                    Ousama = RequireUlong(rangos, "Ousama"),
                    Teiou = RequireUlong(rangos, "Teiou"),
                },
                ColoresRango = RequireColoresRango(roles)
            },
            Emotes = new EmotesConfiguration
            {
                UmaPoints = RequireEmoteIds(emotes, "UmaPoints"),
                Worrysad = RequireEmoteIds(emotes, "Worrysad"),
                ConfessionReaction = RequireUlong(emotes, "ConfessionReaction"),
            },
            PaisTimezones = RequirePaisTimezones(ids),
            FechasEntradaExcepciones = ReadFechasEntradaExcepciones(configuration)
        };
    }

    private static ulong RequireUlong(IConfigurationSection section, string key)
    {
        string? raw = section[key];
        if (string.IsNullOrEmpty(raw) || !ulong.TryParse(raw, out ulong value) || value == 0)
            throw new InvalidOperationException($"Configuración faltante o inválida en appsettings.json: {section.Path}:{key}");
        return value;
    }

    private static EmotesConfiguration.EmoteIds RequireEmoteIds(IConfigurationSection emotes, string key)
    {
        IConfigurationSection section = emotes.GetSection(key);
        return new EmotesConfiguration.EmoteIds(RequireUlong(section, "Prod"), RequireUlong(section, "Test"));
    }

    private static IReadOnlyList<ColorRangoConfiguration> RequireColoresRango(IConfigurationSection roles)
    {
        var raw = roles.GetSection("ColoresRango").Get<ColorRangoRaw[]>();
        if (raw is null || raw.Length == 0)
            throw new InvalidOperationException("Configuración faltante o inválida en appsettings.json: Ids:Roles:ColoresRango");
        return raw.Select(r =>
        {
            if (!ulong.TryParse(r.RoleId, out ulong roleId))
                throw new InvalidOperationException($"RoleId inválido en Ids:Roles:ColoresRango: {r.RoleId}");
            return new ColorRangoConfiguration(roleId, r.Nombre, r.Rango);
        }).ToList().AsReadOnly();
    }

    private static IReadOnlyList<PaisTimezoneConfiguration> RequirePaisTimezones(IConfigurationSection ids)
    {
        var raw = ids.GetSection("PaisTimezones").Get<PaisTimezoneRaw[]>();
        if (raw is null || raw.Length == 0)
            throw new InvalidOperationException("Configuración faltante o inválida en appsettings.json: Ids:PaisTimezones");
        return raw.Select(r =>
        {
            if (!ulong.TryParse(r.RoleId, out ulong roleId))
                throw new InvalidOperationException($"RoleId inválido en Ids:PaisTimezones: {r.RoleId}");
            return new PaisTimezoneConfiguration(roleId, r.Timezone);
        }).ToList().AsReadOnly();
    }

    private static IReadOnlyList<FechaEntradaExcepcion> ReadFechasEntradaExcepciones(IConfiguration configuration)
    {
        var raw = configuration.GetSection("FechasEntradaExcepciones").Get<FechaEntradaExcepcionRaw[]>();
        if (raw is null)
            return Array.Empty<FechaEntradaExcepcion>();
        return raw
            .Where(r => ulong.TryParse(r.UserId, out ulong userId) && userId != 0)
            .Select(r =>
            {
                ulong userId = ulong.Parse(r.UserId);
                if (!DateTimeOffset.TryParseExact(r.FechaEntrada, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTimeOffset fecha))
                    throw new InvalidOperationException($"FechaEntrada inválida en FechasEntradaExcepciones (formato esperado yyyy/MM/dd HH:mm:ss): {r.FechaEntrada}");
                return new FechaEntradaExcepcion(r.Nombre ?? string.Empty, userId, fecha);
            })
            .ToList()
            .AsReadOnly();
    }

    private record ColorRangoRaw(string RoleId, string Nombre, string Rango);
    private record PaisTimezoneRaw(string RoleId, string Timezone);
    private record FechaEntradaExcepcionRaw(string? Nombre, string UserId, string FechaEntrada);
}
