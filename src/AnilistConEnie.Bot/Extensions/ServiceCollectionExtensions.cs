using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredDiscordClient(
        this IServiceCollection services)
    {
        services.AddSingleton<DiscordClient>(provider =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
            Events events = provider.GetRequiredService<Events>();
            
            string token = configuration.GetValue<string>("discordToken") ?? throw new InvalidOperationException("No se encontró el token de Discord en la configuración.");
            
            DiscordClientBuilder clientBuilder = DiscordClientBuilder.CreateDefault(token, DiscordIntents.All);

            clientBuilder.ConfigureEventHandlers(b => b
                .HandleMessageCreated(events.MessageCreated)
                .HandleGuildDownloadCompleted(events.GuildDownloadCompleted));
            
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

            DiscordClient client = clientBuilder.Build();

            return client;
        });

        return services;
    }
}