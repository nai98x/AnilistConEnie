using Microsoft.Extensions.Configuration;

namespace AnilistConEnie.Bot.Configuration;

public class BotConfiguration
{
    public required ulong GuildId { get; init; }
    public required ulong OwnerId { get; init; }
    public required ChannelConfiguration Channels { get; init; }

    public class ChannelConfiguration
    {
        public required ulong General { get; init; }
        public required ulong ConfigBots { get; init; }
        public required ulong Sugerencias { get; init; }
        public required ulong LogChannelInfo { get; init; }
        public required ulong LogChannelError { get; init; }
        public required ulong Perfiles { get; init; }
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

    public static BotConfiguration FromConfiguration(IConfiguration configuration)
    {
        IConfigurationSection ids = configuration.GetSection("Ids");
        IConfigurationSection channels = ids.GetSection("Channels");
        IConfigurationSection intercambios = channels.GetSection("Intercambios");

        return new BotConfiguration
        {
            GuildId = RequireUlong(ids, "GuildId"),
            OwnerId = RequireUlong(ids, "OwnerId"),
            Channels = new ChannelConfiguration
            {
                General = RequireUlong(channels, "General"),
                ConfigBots = RequireUlong(channels, "ConfigBots"),
                Sugerencias = RequireUlong(channels, "Sugerencias"),
                LogChannelInfo = RequireUlong(channels, "LogChannelInfo"),
                LogChannelError = RequireUlong(channels, "LogChannelError"),
                Perfiles = RequireUlong(channels, "Perfiles"),
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
            }
        };
    }

    private static ulong RequireUlong(IConfigurationSection section, string key)
    {
        string? raw = section[key];
        if (string.IsNullOrEmpty(raw) || !ulong.TryParse(raw, out ulong value) || value == 0)
            throw new InvalidOperationException($"Configuración faltante o inválida en appsettings.json: {section.Path}:{key}");
        return value;
    }
}
