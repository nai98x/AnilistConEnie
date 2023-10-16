using Microsoft.Extensions.Configuration;

namespace AnilistConEnie.Infrastructure
{
    public class Settings
    {
        public ulong Id { get; private set; }
        public ulong TestId { get; private set; }
        public ulong TamaRole { get; private set; }
        public ulong CasualRole { get; private set; }
        public ulong KouhaiRole { get; private set; }
        public ulong SenpaiRole { get; private set; }
        public ulong HikikomoriRole { get; private set; }
        public ulong SenseiRole { get; private set; }
        public ulong OusamaRole { get; private set; }
        public ulong TeiouRole { get; private set; }
        public ulong IntercambioForum { get; private set; }
        public ulong AnimeIntercambioChannel { get; private set; }
        public ulong MangaIntercambioChannel { get; private set; }
        public ulong PelisIntercambioChannel { get; private set; }
        public ulong SeriesIntercambioChannel { get; private set; }
        public ulong MusicaIntercambioChannel { get; private set; }
        public ulong AnimeIntercambioTag { get; private set; }
        public ulong MangaIntercambioTag { get; private set; }
        public ulong PelisIntercambioTag { get; private set; }
        public ulong SeriesIntercambioTag { get; private set; }
        public ulong MusicaIntercambioTag { get; private set; }

        private readonly IConfiguration _configuration;

        public Settings(IConfiguration configuration)
        {
            _configuration = configuration;

            Id = _configuration.GetValue<ulong>("guildSettings:id");
            TestId = _configuration.GetValue<ulong>("guildSettings:testId");
            CasualRole = _configuration.GetValue<ulong>("guildSettings:roles:casual");
            KouhaiRole = _configuration.GetValue<ulong>("guildSettings:roles:kouhai");
            SenpaiRole = _configuration.GetValue<ulong>("guildSettings:roles:senpai");
            HikikomoriRole = _configuration.GetValue<ulong>("guildSettings:roles:hikikomori");
            SenseiRole = _configuration.GetValue<ulong>("guildSettings:roles:sensei");
            OusamaRole = _configuration.GetValue<ulong>("guildSettings:roles:ousama");
            TeiouRole = _configuration.GetValue<ulong>("guildSettings:roles:teiou");
            IntercambioForum = configuration.GetValue<ulong>("guildSettings:intercambiosForum");
            AnimeIntercambioChannel = _configuration.GetValue<ulong>("guildSettings:canalesIntercambios:anime");
            MangaIntercambioChannel = _configuration.GetValue<ulong>("guildSettings:canalesIntercambios:manga");
            PelisIntercambioChannel = _configuration.GetValue<ulong>("guildSettings:canalesIntercambios:pelis");
            SeriesIntercambioChannel = _configuration.GetValue<ulong>("guildSettings:canalesIntercambios:series");
            MusicaIntercambioChannel = _configuration.GetValue<ulong>("guildSettings:canalesIntercambios:musica");
            AnimeIntercambioTag = _configuration.GetValue<ulong>("guildSettings:tagsIntercambios:anime");
            MangaIntercambioTag = _configuration.GetValue<ulong>("guildSettings:tagsIntercambios:manga");
            PelisIntercambioTag = _configuration.GetValue<ulong>("guildSettings:tagsIntercambios:pelis");
            SeriesIntercambioTag = _configuration.GetValue<ulong>("guildSettings:tagsIntercambios:series");
            MusicaIntercambioTag = _configuration.GetValue<ulong>("guildSettings:tagsIntercambios:musica");
        }
    }
}
