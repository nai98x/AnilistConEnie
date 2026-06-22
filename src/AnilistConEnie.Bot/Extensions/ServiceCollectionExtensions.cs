using AnilistConEnie.Bot.Events;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredDiscordClient(this IServiceCollection services)
    {
        services.AddDiscordEventHandlers();

        services.AddSingleton<DiscordClient>(provider =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();

            string token = configuration.GetValue<string>("discordToken")
                ?? throw new InvalidOperationException("'discordToken' es obligatorio: configuralo via User Secrets (local) o variable de entorno 'discordToken' (servidor)");

            DiscordClientBuilder clientBuilder = DiscordClientBuilder.CreateDefault(token, DiscordIntents.All);

            clientBuilder.BindEventHandlers(provider);

            clientBuilder.UseCommands((_, extension) =>
            {
                //extension.AddCommands([typeof(MyCommand), typeof(MyOtherCommand)]);

                SlashCommandProcessor slashCommandProcessor = new(new SlashCommandConfiguration());
                extension.AddProcessor(slashCommandProcessor);
            }, new CommandsConfiguration()
            {
                RegisterDefaultCommandProcessors = true,
                UseDefaultCommandErrorHandler = false
            });

            return clientBuilder.Build();
        });

        return services;
    }
}
