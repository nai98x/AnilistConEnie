using AnilistConEnie.Model.Entities.Anilist;

namespace AnilistConEnie.Model.Interfaces;

/// <summary>
/// Cliente de alto nivel de la API GraphQL de AniList. Cada método representa una consulta y
/// devuelve el objeto de dominio correspondiente. El manejo de cliente, reintentos, excepciones
/// y rate limit está centralizado en la implementación (capa Infrastructure).
/// </summary>
public interface IAnilistClient
{
    /// <summary>Obtiene un Media (anime/manga) por su id de AniList.</summary>
    /// <returns>El media, o <c>null</c> si la consulta no devolvió datos.</returns>
    Task<AnilistMedia?> GetMediaAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca media por nombre, devolviendo hasta <paramref name="perPage"/> candidatos ordenados por
    /// relevancia (igual que la búsqueda de AniList). La selección entre los resultados es
    /// responsabilidad de quien consume.
    /// </summary>
    Task<IReadOnlyList<AnilistMedia>> SearchMediaAsync(
        string search,
        AnilistMediaType type,
        int perPage = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las entradas de lista (score, estado, progreso) de un media para los usuarios de AniList
    /// indicados. Pensado para lotes; AniList limita a 50 ids por página, así que el llamador debe
    /// trocear (<c>Chunk(50)</c>) si tiene más.
    /// </summary>
    Task<IReadOnlyList<AnilistUserScore>> GetMediaUserScoresAsync(
        int mediaId,
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca un usuario de AniList por nombre.
    /// </summary>
    /// <returns>El usuario, o <c>null</c> si AniList no encontró ninguna coincidencia.</returns>
    Task<AnilistUser?> SearchUserAsync(string search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los datos del usuario autenticado (<c>Viewer</c>) usando su token OAuth. Pensado para
    /// el flujo de vinculación de cuentas.
    /// </summary>
    /// <returns>El usuario autenticado, o <c>null</c> si la consulta no devolvió datos.</returns>
    Task<AnilistViewer?> GetViewerAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene los ids de usuario de AniList que respondieron (reply) a una actividad/post. Se usa
    /// para saber qué usuarios participaron en un challenge a partir del link de su post.
    /// </summary>
    Task<IReadOnlyList<int>> GetActivityReplyUserIdsAsync(int activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta liviana cuyo único objetivo es leer el estado actual del rate limit de AniList.
    /// </summary>
    Task<AnilistRateLimit> GetRateLimitAsync(CancellationToken cancellationToken = default);
}
