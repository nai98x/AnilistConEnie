using AnilistConEnie.Infrastructure.Services.Interface;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AnilistConEnie.Infrastructure.Handlers
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly Settings _settings;

        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services, IConfiguration config, Settings settings)
        {
            _client = client;
            _handler = handler;
            _services = services;
            _configuration = config;
            _settings = settings;
        }

        public async Task InitializeAsync()
        {
            var logService = _services.GetRequiredService<ILogService>();

            _client.Ready += ReadyAsync;
            _handler.Log += logService.LogAsync;

            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteraction;
        }

        private async Task ReadyAsync()
        {
#if DEBUG
            await _handler.RegisterCommandsToGuildAsync(_settings.TestId, true);
#else
            await _handler.RegisterCommandsGloballyAsync(true);
#endif
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);

                var result = await _handler.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            // implement
                            break;
                        default:
                            break;
                    }
            }
            catch
            {
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}
