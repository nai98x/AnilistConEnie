namespace AnilistConEnie.Infrastructure.Anilist;

/// <summary>
/// Repositorio central de las queries GraphQL de AniList. El conjunto de campos de un Media se define
/// una sola vez (<see cref="MediaFields"/>) y se reutiliza tanto en la búsqueda como en el lookup por id.
/// </summary>
internal static class AnilistQueries
{
    /// <summary>Campos de un nodo Media que el cliente sabe mapear a <c>AnilistMedia</c>.</summary>
    private const string MediaFields =
        """
        id
        siteUrl
        description
        format
        status
        episodes
        chapters
        meanScore
        seasonYear
        isAdult
        genres
        synonyms
        bannerImage
        title { romaji english native }
        coverImage { large }
        startDate { year month day }
        endDate { year month day }
        tags { name isMediaSpoiler }
        studios { nodes { name siteUrl } }
        externalLinks { site url }
        """;

    public static readonly string MediaById =
        $$"""
        query ($id: Int) {
            Media(id: $id) {
                {{MediaFields}}
            }
        }
        """;

    public static readonly string SearchMedia =
        $$"""
        query ($search: String, $type: MediaType, $perPage: Int) {
            Page(perPage: $perPage) {
                media(type: $type, search: $search) {
                    {{MediaFields}}
                }
            }
        }
        """;

    /// <summary>Query mínima usada solo para leer los headers de rate limit.</summary>
    public const string RateLimitProbe =
        """
        query {
            Media(id: 1) {
                id
            }
        }
        """;
}
