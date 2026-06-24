namespace AnilistConEnie.Infrastructure.Anilist.Responses;

/// <summary>Respuesta de una query que devuelve un único <c>Media</c> (por id) o el probe de rate limit.</summary>
internal sealed class MediaQueryResponse
{
    public MediaDto? Media { get; set; }
}

/// <summary>Respuesta de la búsqueda paginada: <c>{ "Page": { "media": [ ... ] } }</c>.</summary>
internal sealed class MediaSearchResponse
{
    public MediaPageDto? Page { get; set; }
}

internal sealed class MediaPageDto
{
    public List<MediaDto>? Media { get; set; }
}
