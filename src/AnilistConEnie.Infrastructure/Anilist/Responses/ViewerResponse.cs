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
    public ViewerAvatarDto? Avatar { get; set; }
    public string? BannerImage { get; set; }
    /// <summary>Epoch en segundos (Unix time) de la creación de la cuenta.</summary>
    public long CreatedAt { get; set; }
}

internal sealed class ViewerAvatarDto
{
    public string? Medium { get; set; }
}
