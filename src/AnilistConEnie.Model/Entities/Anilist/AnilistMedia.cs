namespace AnilistConEnie.Model.Entities.Anilist;

/// <summary>
/// Media (anime/manga) de AniList con datos crudos y estructurados. El formateo para presentación
/// (unir géneros, marcar spoilers, armar fechas, etc.) es responsabilidad de la capa que lo consume.
/// </summary>
public class AnilistMedia
{
    public int Id { get; set; }
    public AnilistMediaTitle? Title { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public string SiteUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Format { get; set; }
    public string? Status { get; set; }
    public int? Episodes { get; set; }
    public int? Chapters { get; set; }
    public int? MeanScore { get; set; }
    public int? SeasonYear { get; set; }
    public AnilistFuzzyDate? StartDate { get; set; }
    public AnilistFuzzyDate? EndDate { get; set; }
    public bool IsAdult { get; set; }

    public IReadOnlyList<string> Genres { get; set; } = [];
    public IReadOnlyList<string> Synonyms { get; set; } = [];
    public IReadOnlyList<AnilistMediaTag> Tags { get; set; } = [];
    public IReadOnlyList<AnilistStudio> Studios { get; set; } = [];
    public IReadOnlyList<AnilistExternalLink> ExternalLinks { get; set; } = [];
}

public class AnilistMediaTitle
{
    public string? Romaji { get; set; }
    public string? English { get; set; }
    public string? Native { get; set; }
}

/// <summary>Fecha "difusa" de AniList: cualquiera de sus componentes puede faltar.</summary>
public class AnilistFuzzyDate
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }

    public bool IsComplete => Year is not null && Month is not null && Day is not null;
}

public class AnilistMediaTag
{
    public string Name { get; set; } = string.Empty;
    public bool IsSpoiler { get; set; }
}

public class AnilistStudio
{
    public string Name { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
}

public class AnilistExternalLink
{
    public string Site { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
