namespace AnilistConEnie.Domain.Entities.Anilist;

/// <summary>Usuario de AniList tal como lo devuelve una búsqueda por nombre.</summary>
public record AnilistUser
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SiteUrl { get; init; } = string.Empty;
    public string? AvatarMedium { get; init; }
    public string? BannerImage { get; init; }
}
