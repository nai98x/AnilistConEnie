using AnilistConEnie.Bot.Configuration;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services;

public class DiscordBotService(DiscordClient client, BotConfiguration config, ILogger<DiscordBotService> logger)
    : IHostedService
{
    public bool Debug { get; } = SetDebug();
    public DiscordChannel? LogChannelInfo { get; private set; }
    public DiscordChannel? LogChannelErrors { get; private set; }

    public void SetChannels()
    {
        DiscordGuild guild = client.Guilds[config.GuildId];
        LogChannelInfo = guild.Channels[config.Channels.LogChannelInfo];
        LogChannelErrors = guild.Channels[config.Channels.LogChannelError];
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando bot de Discord");
        await client.ConnectAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Desconectando bot de Discord");
        await client.DisconnectAsync();
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
