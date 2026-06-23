namespace AnilistConEnie.Infrastructure.Anilist.Responses;

/// <summary>Respuesta de la query del usuario autenticado: <c>{ "Viewer": { ... } }</c>.</summary>
internal sealed class ViewerResponse
{
    public ViewerDto? Viewer { get; set; }
}

internal sealed class ViewerDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? SiteUrl { get; set; }
    public AnilistAvatarDto? Avatar { get; set; }
    public string? BannerImage { get; set; }
    /// <summary>Epoch en segundos (Unix time) de la creación de la cuenta.</summary>
    public long CreatedAt { get; set; }
}

/// <summary>Avatar de un usuario de AniList, compartido entre las respuestas de <c>User</c> y <c>Viewer</c>.</summary>
internal sealed class AnilistAvatarDto
{
    public string? Medium { get; set; }
}
