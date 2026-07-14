using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Enum;
using DSharpPlus;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>Beneficios que desbloquea cada rango del servidor.</summary>
public static class RangoBeneficios
{
    public static string? Emoji(RangoEnum rango) => rango switch
    {
        RangoEnum.Tama => "🥚",
        RangoEnum.Kouhai => "🍙",
        RangoEnum.Senpai => "🍜",
        RangoEnum.Ousama => "👑",
        RangoEnum.Teiou => "🥕",
        _ => null,
    };

    public static string? Items(RangoEnum rango, DiscordGuild guild, BotConfiguration config) => rango switch
    {
        RangoEnum.Tama => "- Participar en los intercambios\n- Adjuntar archivos",
        RangoEnum.Kouhai => "- Entrada garantizada a eventos del servidor",
        RangoEnum.Senpai => "- Elegir entre 45 colores para tu usuario",
        RangoEnum.Ousama => "- Canal de voz propio",
        RangoEnum.Teiou => $"- Escribir en {guild.Channels[config.Channels.Teiou].Mention}\n- Comando {Formatter.InlineCode("/teiou nickname")}",
        _ => null,
    };
}
