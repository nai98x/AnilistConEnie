namespace AnilistConEnie.Model.Entities.Anilist;

/// <summary>
/// Usuario autenticado de AniList (<c>Viewer</c>), obtenido con el token OAuth del propio usuario.
/// Se usa al vincular una cuenta para conocer su identidad y antigüedad.
/// </summary>
public class AnilistViewer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public string? AvatarMedium { get; set; }
    public string? BannerImage { get; set; }

    /// <summary>Fecha de creación de la cuenta de AniList.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
