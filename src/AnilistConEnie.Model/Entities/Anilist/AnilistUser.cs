namespace AnilistConEnie.Model.Entities.Anilist;

/// <summary>Usuario de AniList tal como lo devuelve una búsqueda por nombre.</summary>
public class AnilistUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public string? AvatarMedium { get; set; }
    public string? BannerImage { get; set; }
}
