using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>Embed reutilizable para informar al usuario que la API de AniList no respondió (5xx / caída).</summary>
public static class AnilistErrorEmbed
{
    public static DiscordEmbedBuilder NoDisponible() =>
        new()
        {
            Title = "AniList no disponible",
            Description = "No se pudo consultar AniList: la API probablemente esté caída. Probá de nuevo en unos minutos.",
            Color = DiscordColor.Orange
        };
}
