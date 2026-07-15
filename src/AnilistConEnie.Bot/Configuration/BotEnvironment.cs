namespace AnilistConEnie.Bot.Configuration;

/// <summary>Entorno de ejecución del bot: debug corre contra la aplicación y los emotes de test.</summary>
public static class BotEnvironment
{
#if DEBUG
    public const bool EsDebug = true;
#else
    public const bool EsDebug = false;
#endif
}
