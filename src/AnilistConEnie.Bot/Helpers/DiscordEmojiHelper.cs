using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>Utilidades visuales del bot: color de marca y resolución de emojis.</summary>
public static class DiscordEmojiHelper
{
    public static DiscordColor GetColor() => DiscordColor.Blurple;

    /// <summary>
    /// Resuelve un emoji propiedad de la aplicación (bot), no del guild. Los emotes configurados del
    /// bot están subidos a nivel de aplicación, por lo que <c>FromGuildEmote</c> y
    /// <c>guild.Emojis[...]</c> no los encuentran. Usa la caché de la librería (no fuerza fetch).
    /// </summary>
    public static ValueTask<DiscordEmoji> GetApplicationEmojiAsync(DiscordClient client, ulong emojiId)
        => client.GetApplicationEmojiAsync(emojiId, false);

    /// <summary>
    /// Convierte el texto de un emoji (un emoji custom <c>&lt;:nombre:id&gt;</c> / <c>&lt;a:nombre:id&gt;</c>
    /// o un emoji unicode) en un <see cref="DiscordEmoji"/> usable, o <c>null</c> si no se puede resolver.
    /// </summary>
    public static DiscordEmoji? ToEmoji(DiscordClient client, string text)
    {
        text = text.Trim();
        Match match = Regex.Match(text, @"^<?a?:?([a-zA-Z0-9_]+):([0-9]+)>?$");
        if (!match.Success)
            return DiscordEmoji.TryFromUnicode(text, out DiscordEmoji? emoji) ? emoji : null;

        return ulong.TryParse(match.Groups[2].Value, out ulong id) && DiscordEmoji.TryFromGuildEmote(client, id, out DiscordEmoji? guildEmote)
            ? guildEmote
            : null;
    }

    /// <summary>
    /// Encuentra todos los emotes personalizados (<c>&lt;:nombre:id&gt;</c> / <c>&lt;a:nombre:id&gt;</c>)
    /// embebidos en un texto largo, sin duplicados por id. No resuelve contra ningún guild: solo
    /// extrae nombre/id/animado tal como aparecen en el texto.
    /// </summary>
    public static IEnumerable<(string Name, ulong Id, bool Animated)> FindAllCustomEmotes(string content) =>
        Regex.Matches(content, @"<(a)?:(.+?):(\d+)>")
            .Select(m => (Name: m.Groups[2].Value, Id: ulong.Parse(m.Groups[3].Value), Animated: m.Groups[1].Success))
            .DistinctBy(e => e.Id);
}
