using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Events;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
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
        
        services.AddDiscordClient(token, DiscordIntents.All);
        
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

            SlashCommandProcessor slashCommandProcessor = new(new SlashCommandConfiguration());
            extension.AddProcessor(slashCommandProcessor);
        }, new CommandsConfiguration
        {
            RegisterDefaultCommandProcessors = true,
            UseDefaultCommandErrorHandler = true
        });

        return services;
    }
}
