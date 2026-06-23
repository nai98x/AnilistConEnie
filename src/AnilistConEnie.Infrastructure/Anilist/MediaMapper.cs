using AnilistConEnie.Infrastructure.Anilist.Responses;
using AnilistConEnie.Model.Entities.Anilist;

namespace AnilistConEnie.Infrastructure.Anilist;

/// <summary>
/// Convierte los DTOs de transporte de AniList al modelo de dominio. Concentra acá el mapeo (en vez
/// de decodificar dinámicamente como en el código viejo) mantiene el dominio limpio y tipado.
/// </summary>
internal static class MediaMapper
{
    public static AnilistMedia ToDomain(MediaDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title is null ? null : new AnilistMediaTitle
        {
            Romaji = dto.Title.Romaji,
            English = dto.Title.English,
            Native = dto.Title.Native
        },
        CoverImageUrl = dto.CoverImage?.Large,
        BannerImageUrl = dto.BannerImage,
        SiteUrl = dto.SiteUrl ?? string.Empty,
        Description = dto.Description,
        Format = dto.Format,
        Status = dto.Status,
        Episodes = dto.Episodes,
        Chapters = dto.Chapters,
        MeanScore = dto.MeanScore,
        SeasonYear = dto.SeasonYear,
        StartDate = ToFuzzyDate(dto.StartDate),
        EndDate = ToFuzzyDate(dto.EndDate),
        IsAdult = dto.IsAdult,
        Genres = dto.Genres ?? [],
        Synonyms = dto.Synonyms ?? [],
        Tags = dto.Tags?.Select(t => new AnilistMediaTag
        {
            Name = t.Name ?? string.Empty,
            IsSpoiler = t.IsMediaSpoiler
        }).ToList() ?? [],
        Studios = dto.Studios?.Nodes?.Select(s => new AnilistStudio
        {
            Name = s.Name ?? string.Empty,
            SiteUrl = s.SiteUrl ?? string.Empty
        }).ToList() ?? [],
        ExternalLinks = dto.ExternalLinks?.Select(l => new AnilistExternalLink
        {
            Site = l.Site ?? string.Empty,
            Url = l.Url ?? string.Empty
        }).ToList() ?? []
    };

    public static AnilistUserScore ToUserScore(MediaListEntryDto dto) => new()
    {
        UserId = dto.User?.Id ?? 0,
        UserName = dto.User?.Name ?? string.Empty,
        UserSiteUrl = dto.User?.SiteUrl ?? string.Empty,
        ScoreFormat = dto.User?.MediaListOptions?.ScoreFormat,
        RawScore = dto.Score,
        Score100 = dto.Score100,
        Status = dto.Status ?? string.Empty,
        Progress = dto.Progress
    };

    public static AnilistUser ToUser(UserDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name ?? string.Empty,
        SiteUrl = dto.SiteUrl ?? string.Empty,
        AvatarMedium = dto.Avatar?.Medium,
        BannerImage = dto.BannerImage
    };

    public static AnilistViewer ToViewer(ViewerDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name ?? string.Empty,
        SiteUrl = dto.SiteUrl ?? string.Empty,
        AvatarMedium = dto.Avatar?.Medium,
        BannerImage = dto.BannerImage,
        CreatedAt = DateTimeOffset.FromUnixTimeSeconds(dto.CreatedAt)
    };

    private static AnilistFuzzyDate? ToFuzzyDate(FuzzyDateDto? dto) =>
        dto is null ? null : new AnilistFuzzyDate { Year = dto.Year, Month = dto.Month, Day = dto.Day };
}
