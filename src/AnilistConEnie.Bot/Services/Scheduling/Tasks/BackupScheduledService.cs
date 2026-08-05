using AnilistConEnie.Application.Backups;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Configuration;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services.Scheduling.Tasks;

public class BackupScheduledService(
    IServiceScopeFactory scopeFactory,
    ILogger<BackupScheduledService> logger,
    DiscordClient client,
    BotConfiguration config,
    BackupsSettings settings,
    DiscordBotService discordBotService)
    : CronBackgroundService(scopeFactory, discordBotService, logger)
{
    protected override string CronExpression => "0 9 * * *";

    protected override async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        if (!Inicializado || !client.Guilds.TryGetValue(config.GuildId, out DiscordGuild? guild))
            return;

        string? marca = File.Exists(settings.RutaEstado)
            ? await File.ReadAllTextAsync(settings.RutaEstado, cancellationToken)
            : null;

        if (EstadoBackup.EstaAlDia(marca, DateOnly.FromDateTime(RelojServidor.Hoy)))
            return;

        await guild.Channels[config.Channels.ConfigBots].SendMessageAsync(
            $"⚠️ No se detectó el backup de la base de hoy. Último backup correcto: {EstadoBackup.DescribirUltimo(marca)}.");
    }
}
