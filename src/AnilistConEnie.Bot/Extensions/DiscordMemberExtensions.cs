using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Extensions;

public static class DiscordMemberExtensions
{
    // Prefiere el avatar propio del servidor y cae al avatar global si no tiene uno específico del guild.
    public static string AvatarUrlPreferido(this DiscordMember member) =>
        member.GuildAvatarUrl ?? member.AvatarUrl;
}
