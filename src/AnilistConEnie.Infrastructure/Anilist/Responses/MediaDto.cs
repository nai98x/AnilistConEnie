namespace AnilistConEnie.Infrastructure.Anilist.Responses;

/// <summary>
/// DTO de transporte que refleja exactamente la forma del nodo <c>Media</c> en la respuesta GraphQL
/// de AniList. Newtonsoft lo deserializa por nombre (case-insensitive); luego <c>MediaMapper</c> lo
/// convierte al modelo de dominio limpio.
/// </summary>
internal sealed class MediaDto
{
    public int Id { get; set; }
    public MediaTitleDto? Title { get; set; }
    public CoverImageDto? CoverImage { get; set; }
    public string? BannerImage { get; set; }
    public string? SiteUrl { get; set; }
    public string? Description { get; set; }
    public string? Format { get; set; }
    public string? Status { get; set; }
    public int? Episodes { get; set; }
    public int? Chapters { get; set; }
    public int? MeanScore { get; set; }
    public int? SeasonYear { get; set; }
    public FuzzyDateDto? StartDate { get; set; }
    public FuzzyDateDto? EndDate { get; set; }
    public bool IsAdult { get; set; }
    public List<string>? Genres { get; set; }
    public List<string>? Synonyms { get; set; }
    public List<TagDto>? Tags { get; set; }
    public StudioConnectionDto? Studios { get; set; }
    public List<ExternalLinkDto>? ExternalLinks { get; set; }
}

internal sealed class MediaTitleDto
{
    public string? Romaji { get; set; }
    public string? English { get; set; }
    public string? Native { get; set; }
}

internal sealed class CoverImageDto
{
    public string? Large { get; set; }
}

internal sealed class FuzzyDateDto
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
}

internal sealed class TagDto
{
    public string? Name { get; set; }
    public bool IsMediaSpoiler { get; set; }
}

internal sealed class StudioConnectionDto
{
    public List<StudioNodeDto>? Nodes { get; set; }
}

internal sealed class StudioNodeDto
{
    public string? Name { get; set; }
    public string? SiteUrl { get; set; }
}

internal sealed class ExternalLinkDto
{
    public string? Site { get; set; }
    public string? Url { get; set; }
}
