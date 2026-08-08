using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Domain.Enum;
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
        RangoEnum.Senpai => $"- Elegir entre {CantidadColores(config, RangoEnum.Senpai)} colores para tu usuario",
        RangoEnum.Ousama => $"- Canal de voz propio\n- Elegir entre {CantidadColores(config, RangoEnum.Ousama)} colores con degradado",
        RangoEnum.Teiou => $"- Escribir en {guild.Channels[config.Channels.Teiou].Mention}\n- Comando {Formatter.InlineCode("/teiou nickname")}\n- Color holográfico",
        _ => null,
    };

    private static int CantidadColores(BotConfiguration config, RangoEnum rango) =>
        config.Roles.ColoresRango.Count(c => c.Rango == rango.ToString());
}
