using AnilistConEnie.Bot.Configuration;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services;

public class MainService(DiscordClient client, BotConfiguration config, ILogger<MainService> logger)
{
    public bool Debug { get; } = SetDebug();
    public bool Initialized { get; private set; } = false;
    public DiscordChannel? LogChannelInfo { get; private set; }
    public DiscordChannel? LogChannelErrors { get; private set; }

    public void SetInitialized()
    {
        Initialized = true;
        logger.LogInformation("Bot inicializado correctamente");
    }

    public void SetChannels()
    {
        DiscordGuild guild = client.Guilds[config.GuildId];
        LogChannelInfo = guild.Channels[config.Channels.LogChannelInfo];
        LogChannelErrors = guild.Channels[config.Channels.LogChannelError];
    }

    private static bool SetDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
