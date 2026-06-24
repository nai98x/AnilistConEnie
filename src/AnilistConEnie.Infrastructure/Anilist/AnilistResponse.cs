using AnilistConEnie.Model.Entities.Anilist;

namespace AnilistConEnie.Infrastructure.Anilist;

/// <summary>
/// Resultado interno que el executor devuelve a <c>AnilistClient</c>: los datos deserializados
/// más el estado de rate limit leído de los headers de esa misma respuesta.
/// </summary>
internal sealed record AnilistResponse<T>(T? Data, AnilistRateLimit RateLimit);
