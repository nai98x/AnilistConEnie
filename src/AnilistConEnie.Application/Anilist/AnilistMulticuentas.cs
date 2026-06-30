using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Anilist;

public sealed record GrupoMulticuenta(int AnilistId, IReadOnlyList<long> DiscordIds);

public static class AnilistMulticuentas
{
    /// <summary>
    /// Agrupa los Discord vinculados por id de AniList, preservando el orden de aparición y sin
    /// repetir un mismo Discord dentro del perfil.
    /// </summary>
    public static Dictionary<int, List<long>> AgruparPorAnilistId(IReadOnlyList<UsuarioAnilist> vinculados)
    {
        Dictionary<int, List<long>> porAnilistId = [];
        foreach (UsuarioAnilist usuario in vinculados)
        {
            if (!AnilistProfileUrl.TryGetUserId(usuario.AnilistURL, out int anilistId)) continue;

            List<long> ids = porAnilistId.TryGetValue(anilistId, out List<long>? existentes) ? existentes : porAnilistId[anilistId] = [];
            if (!ids.Contains(usuario.UserId))
                ids.Add(usuario.UserId);
        }

        return porAnilistId;
    }

    /// <summary>
    /// De los perfiles vinculados, devuelve aquellos con más de un Discord distinto vinculado,
    /// ordenados de mayor a menor cantidad de cuentas.
    /// </summary>
    public static IReadOnlyList<GrupoMulticuenta> Detectar(IReadOnlyList<UsuarioAnilist> vinculados) =>
        AgruparPorAnilistId(vinculados)
            .Where(x => x.Value.Count > 1)
            .OrderByDescending(x => x.Value.Count)
            .Select(x => new GrupoMulticuenta(x.Key, x.Value))
            .ToList();
}
