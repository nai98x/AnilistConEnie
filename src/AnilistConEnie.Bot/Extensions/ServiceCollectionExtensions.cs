using AnilistConEnie.Bot.Commands.Framework.Checks;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Events;
using AnilistConEnie.Bot.Helpers;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Entities;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredDiscordClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDiscordEventHandlers();

        string token = configuration.GetValue<string>("discordToken")
            ?? throw new InvalidOperationException("'discordToken' es obligatorio: configuralo via User Secrets (local) o variable de entorno 'discordToken' (servidor)");

        services.AddDiscordClient(token,
            DiscordIntents.Guilds
            | DiscordIntents.GuildMembers
            | DiscordIntents.GuildPresences
            | DiscordIntents.GuildMessages
            | DiscordIntents.GuildMessageReactions
            | DiscordIntents.MessageContents);

        services.AddInteractivityExtension(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(5),
            ButtonBehavior = ButtonPaginationBehavior.Disable
        });

        services.ConfigureEventHandlers(events => events.BindEventHandlers());

        services.AddCommandsExtension((provider, extension) =>
        {
            BotConfiguration config = provider.GetRequiredService<BotConfiguration>();
            extension.AddDiscoveredSlashCommands(config.GuildId);
            extension.AddDiscoveredContextMenuCommands(config.GuildId);
            extension.AddDiscoveredTextCommands();

            extension.AddCheck<RequireKamiSamaCheck>();

            SlashCommandProcessor slashCommandProcessor = new(new SlashCommandConfiguration());
            extension.AddProcessor(slashCommandProcessor);

            TextCommandProcessor textCommandProcessor = new(new TextCommandConfiguration
            {
                PrefixResolver = ResolveMentionPrefixAsync
            });
            extension.AddProcessor(textCommandProcessor);

            DiscordLogService logService = provider.GetRequiredService<DiscordLogService>();
            extension.CommandExecuted += (_, args) => logService.GrabarLogComandoEjecutado(args.Context);
        }, new CommandsConfiguration
        {
            RegisterDefaultCommandProcessors = true,
            UseDefaultCommandErrorHandler = true
        });

        return services;
    }

    /// <summary>
    /// Resuelve comandos de texto solo por mención al bot (sin prefijo tipo "!"). El
    /// <c>DefaultPrefixResolver</c> de DSharpPlus no soporta "solo mención": su constructor exige al
    /// menos un prefijo no vacío. Misma lógica de mención que usa ese default internamente.
    /// </summary>
    private static ValueTask<int> ResolveMentionPrefixAsync(CommandsExtension extension, DiscordMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Content)) return ValueTask.FromResult(-1);

        string mention = extension.Client.CurrentUser.Mention;
        return ValueTask.FromResult(
            message.Content.StartsWith(mention, StringComparison.OrdinalIgnoreCase) ? mention.Length : -1);
    }
}
