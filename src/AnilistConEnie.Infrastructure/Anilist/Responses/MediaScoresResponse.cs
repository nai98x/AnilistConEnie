namespace AnilistConEnie.Infrastructure.Anilist.Responses;

/// <summary>Respuesta de la query de scores: <c>{ "Page": { "mediaList": [ ... ] } }</c>.</summary>
internal sealed class MediaScoresResponse
{
    public ScorePageDto? Page { get; set; }
}

internal sealed class ScorePageDto
{
    public List<MediaListEntryDto>? MediaList { get; set; }
}

internal sealed class MediaListEntryDto
{
    public double Score { get; set; }
    /// <summary>Alias <c>score100</c> de la query: el score normalizado a /100 por AniList.</summary>
    public double Score100 { get; set; }
    public string? Status { get; set; }
    public int? Progress { get; set; }
    public ScoreUserDto? User { get; set; }
}

internal sealed class ScoreUserDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? SiteUrl { get; set; }
    public ScoreUserOptionsDto? MediaListOptions { get; set; }
}

internal sealed class ScoreUserOptionsDto
{
    public string? ScoreFormat { get; set; }
}
