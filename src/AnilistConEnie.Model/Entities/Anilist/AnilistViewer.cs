namespace AnilistConEnie.Model.Entities.Anilist;

/// <summary>
/// Usuario autenticado de AniList (<c>Viewer</c>), obtenido con el token OAuth del propio usuario.
/// Se usa al vincular una cuenta para conocer su identidad y antigüedad.
/// </summary>
public record AnilistViewer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SiteUrl { get; init; } = string.Empty;
    public string? AvatarMedium { get; init; }
    public string? BannerImage { get; init; }

    /// <summary>Fecha de creación de la cuenta de AniList.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
