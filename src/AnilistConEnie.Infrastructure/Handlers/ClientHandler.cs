using Discord.WebSocket;
using Discord;
using Serilog;
using AnilistConEnie.Infrastructure.Services.Interface;
using AnilistConEnie.Infrastructure.Helpers.Interface;

namespace AnilistConEnie.Infrastructure.Handlers
{
    public class ClientHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly ICacheService _cacheService;
        private readonly IIntercambioRepostHelper _intercambioRepostHelper;
        private readonly ITriggerHelper _triggerHelper;

        public ClientHandler(DiscordSocketClient client, ICacheService cacheService, IIntercambioRepostHelper intercambioRepostHelper, ITriggerHelper triggerHelper) 
        {
            _client = client;
            _cacheService = cacheService;
            _intercambioRepostHelper = intercambioRepostHelper;
            _triggerHelper = triggerHelper;
        }

        public void ConfigureHandlers()
        {
            _client.Ready += Client_Ready;
            _client.GuildMemberUpdated += Client_GuildMemberUpdated;
            //_client.GuildMemberRemoved
            _client.ReactionAdded += Client_ReactionAdded;
            _client.MessageReceived += Client_MessageReceived;
            _client.MessageUpdated += Client_MessageUpdated;
            _client.MessageDeleted += Client_MessageDeleted;
            //_client.VoiceServerUpdated += Client_VoiceServerUpdated;
        }

        public Task Client_VoiceServerUpdated(SocketVoiceServer arg)
        {
            Log.Information("Client_VoiceServerUpdated");

            return Task.CompletedTask;
        }

        public Task Client_MessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
        {
            _ = _intercambioRepostHelper.DeletePost(arg1, arg2);
            Log.Information("Client_MessageDeleted");
            return Task.CompletedTask;
        }

        public Task Client_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            _ = _intercambioRepostHelper.UpdatePost(arg1, arg2, arg3);
            Log.Information("Client_MessageUpdated");
            return Task.CompletedTask;
        }

        public Task Client_MessageReceived(SocketMessage arg)
        {
            _ = _triggerHelper.ExecuteTrigger(arg);
            _ = _intercambioRepostHelper.AddPost(arg);
            Log.Information("Client_MessageReceived");
            return Task.CompletedTask;
        }

        public Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
        {
            Log.Information("Client_ReactionAdded");

            return Task.CompletedTask;
        }

        public Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
        {
            Log.Information("Client_GuildMemberUpdated");

            return Task.CompletedTask;
        }

        public Task Client_Ready()
        {
            _ = _triggerHelper.SetTriggers(true);

            Log.Information("Client_Ready");
            return Task.CompletedTask;
        }
    }
}
