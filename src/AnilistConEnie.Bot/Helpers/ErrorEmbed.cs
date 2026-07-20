using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

public static class ErrorEmbed
{
    public static DiscordEmbedBuilder De(string descripcion) => De("Error", descripcion);

    public static DiscordEmbedBuilder De(string titulo, string descripcion) =>
        new()
        {
            Title = titulo,
            Description = descripcion,
            Color = DiscordColor.Red
        };

    public static DiscordEmbedBuilder SinPermiso(
        string descripcion = "Necesitas el permiso de `Gestionar servidor` para usar este comando.") =>
        De("Sin permiso", descripcion);
}
