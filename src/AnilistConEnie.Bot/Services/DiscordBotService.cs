using AnilistConEnie.Bot.Configuration;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services;

public class DiscordBotService(DiscordClient client, BotConfiguration config, ILogger<DiscordBotService> logger)
    : IHostedService
{
    public DiscordClient Client => client;
    public bool Debug { get; } = SetDebug();
    public bool Inicializado { get; private set; } = false;
    public bool ErrorInicializacion { get; private set; } = false;

    private DiscordChannel? _logChannelInfo;
    private DiscordChannel? _logChannelErrors;
    private DiscordChannel? _playroom;

    public DiscordChannel LogChannelInfo  => _logChannelInfo  ?? throw new InvalidOperationException($"Canal {nameof(LogChannelInfo)} no inicializado.");
    public DiscordChannel LogChannelErrors => _logChannelErrors ?? throw new InvalidOperationException($"Canal {nameof(LogChannelErrors)} no inicializado.");
    public DiscordChannel Playroom        => _playroom        ?? throw new InvalidOperationException($"Canal {nameof(Playroom)} no inicializado.");

    public void SetChannels()
    {
        DiscordGuild guild = client.Guilds[config.GuildId];
        _logChannelInfo   = guild.Channels[config.Channels.LogChannelInfo]  ?? throw new InvalidOperationException($"Canal LogChannelInfo ({config.Channels.LogChannelInfo}) no encontrado en el guild.");
        _logChannelErrors = guild.Channels[config.Channels.LogChannelError] ?? throw new InvalidOperationException($"Canal LogChannelError ({config.Channels.LogChannelError}) no encontrado en el guild.");
        _playroom         = guild.Channels[config.Channels.Playroom]        ?? throw new InvalidOperationException($"Canal Playroom ({config.Channels.Playroom}) no encontrado en el guild.");
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

    public void SetInicializado()
    {
        Inicializado = true;
    }

    public void SetErrorInicializacion()
    {
        ErrorInicializacion = true;
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
