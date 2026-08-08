using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Extensions;

/// <summary>Seam entre la XP persistida y Discord: acota el ranking a los miembros presentes en el guild.</summary>
public static class XpUsuariosRepositoryExtensions
{
    public static async Task<List<UserXp>> ObtenerRankingDelGuild(this IXpUsuariosRepository repository, DiscordGuild guild)
    {
        List<UserXp> ranking = await repository.ObtenerRanking();
        return ranking.Where(x => guild.Members.ContainsKey((ulong)x.UserId)).ToList();
    }

    /// <summary>Ranking indexado por usuario, para recorrer miembros sin una consulta por cada uno.</summary>
    public static async Task<Dictionary<ulong, UserXp>> ObtenerXpPorUsuario(this IXpUsuariosRepository repository)
    {
        List<UserXp> ranking = await repository.ObtenerRanking();
        return ranking.ToDictionary(x => (ulong)x.UserId);
    }
}
