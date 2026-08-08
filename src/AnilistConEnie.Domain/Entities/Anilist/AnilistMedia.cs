namespace AnilistConEnie.Domain.Entities.Anilist;

/// <summary>
/// Media (anime/manga) de AniList con datos crudos y estructurados. El formateo para presentación
/// (unir géneros, marcar spoilers, armar fechas, etc.) es responsabilidad de la capa que lo consume.
/// </summary>
public record AnilistMedia
{
    public int Id { get; init; }
    public AnilistMediaTitle? Title { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? BannerImageUrl { get; init; }
    public string SiteUrl { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Format { get; init; }
    public string? Status { get; init; }
    public int? Episodes { get; init; }
    public int? Chapters { get; init; }
    public int? MeanScore { get; init; }
    public int? SeasonYear { get; init; }
    public AnilistFuzzyDate? StartDate { get; init; }
    public AnilistFuzzyDate? EndDate { get; init; }
    public bool IsAdult { get; init; }

    public IReadOnlyList<string> Genres { get; init; } = [];
    public IReadOnlyList<string> Synonyms { get; init; } = [];
    public IReadOnlyList<AnilistMediaTag> Tags { get; init; } = [];
    public IReadOnlyList<AnilistStudio> Studios { get; init; } = [];
    public IReadOnlyList<AnilistExternalLink> ExternalLinks { get; init; } = [];
}

public record AnilistMediaTitle
{
    public string? Romaji { get; init; }
    public string? English { get; init; }
    public string? Native { get; init; }
}

/// <summary>Fecha "difusa" de AniList: cualquiera de sus componentes puede faltar.</summary>
public record AnilistFuzzyDate
{
    public int? Year { get; init; }
    public int? Month { get; init; }
    public int? Day { get; init; }

    public bool IsComplete => Year is not null && Month is not null && Day is not null;
}

public record AnilistMediaTag
{
    public string Name { get; init; } = string.Empty;
    public bool IsSpoiler { get; init; }
}

public record AnilistStudio
{
    public string Name { get; init; } = string.Empty;
    public string SiteUrl { get; init; } = string.Empty;
}

public record AnilistExternalLink
{
    public string Site { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}
