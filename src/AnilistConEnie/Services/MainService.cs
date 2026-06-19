using DSharpPlus;
using DSharpPlus.Entities;
using Google.Apis.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Services;

public class MainService(DiscordClient client, IConfiguration configuration, ILogger<MainService> logger)
{
    public bool Debug { get; } = SetDebug();
    public bool Initialized { get; private set; } = false;
    public DiscordChannel? LogChannelInfo { get; private set; } 
    public DiscordChannel? LogChannelErrors { get; private set; } 
    
    public void SetInitialized()
    {
        Initialized = true;
        logger.LogInformation("Bot inicializado correctamente");
    }

    public void SetChannels()
    {
        DiscordGuild guild = client.Guilds[ulong.Parse(configuration["Ids:GuildId"] ?? throw new Exception("Es necesario configurar el Id del servidor"))];
        LogChannelInfo = guild.Channels[ulong.Parse(configuration["Ids:Channels:LogChannelInfo"] ?? throw new Exception("Es necesario configurar el Id del canal de logs info"))];
        LogChannelErrors = guild.Channels[ulong.Parse(configuration["Ids:Channels:LogChannelError"] ?? throw new Exception("Es necesario configurar el Id del canal de logs errores"))];
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