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
    public bool Debug => BotEnvironment.EsDebug;
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
        _logChannelInfo   = GetChannel(guild, config.Channels.LogChannelInfo, "LogChannelInfo");
        _logChannelErrors = GetChannel(guild, config.Channels.LogChannelError, "LogChannelError");
        _playroom         = GetChannel(guild, config.Channels.Playroom, "Playroom");
    }

    private static DiscordChannel GetChannel(DiscordGuild guild, ulong id, string nombre) =>
        guild.Channels.TryGetValue(id, out DiscordChannel? channel)
            ? channel
            : throw new InvalidOperationException($"Canal {nombre} ({id}) no encontrado en el guild.");

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
}
