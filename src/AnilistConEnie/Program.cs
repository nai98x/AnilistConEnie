using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using AnilistConEnie.Infrastructure.Services.Interface;
using AnilistConEnie.Infrastructure.Services.Impl;
using AnilistConEnie.Infrastructure.Handlers;
using Microsoft.Extensions.Configuration;
using Discord.Interactions;
using AnilistConEnie.Infrastructure.Helpers.Interface;
using AnilistConEnie.Infrastructure.Helpers.Impl;
using AnilistConEnie.Infrastructure;
using AnilistConEnie.Infrastructure.Repositories.Interface;
using AnilistConEnie.Infrastructure.Repositories.Impl;

public class Program
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _services;

    private readonly DiscordSocketConfig _socketConfig = new()
    {
        GatewayIntents = GatewayIntents.All,
        AlwaysDownloadUsers = true
    };

    public Program()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        _services = new ServiceCollection()
            .AddSingleton(_configuration)
            .AddSingleton(_socketConfig)
            .AddSingleton<Settings>()
            .AddSingleton<ICommonHelper, CommonHelper>()
            .AddSingleton<IIntercambioRepostHelper, IntercambioRepostHelper>()
            .AddSingleton<ITriggerHelper, TriggerHelper>()
            .AddSingleton<IVoiceChannelHelper, VoiceChannelHelper>()
            .AddSingleton<ILogService, LogService>()
            .AddSingleton<ICacheService, CacheService>()
            .AddSingleton<IIntercambioRepostRepository, IntercambioRepostRepository>()
            .AddSingleton<ITriggerRepository, TriggerRepository>()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<ClientHandler>()
            .AddSingleton<InteractionHandler>()
            .BuildServiceProvider();
    }

    static void Main(string[] args)
        => new Program().RunAsync()
            .GetAwaiter()
            .GetResult();

    async Task RunAsync()
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();
        var logService = _services.GetRequiredService<ILogService>();

        logService.ConfigureLogging();
        client.Log += logService.LogAsync;

        await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

        await client.LoginAsync(TokenType.Bot, _configuration["token"]);
        await client.StartAsync();

        _services.GetRequiredService<ClientHandler>()
            .ConfigureHandlers();

        await Task.Delay(Timeout.Infinite);
    }
}