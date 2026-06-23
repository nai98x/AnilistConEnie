namespace AnilistConEnie.Infrastructure.Anilist.Responses;

/// <summary>Respuesta de la búsqueda de usuario: <c>{ "User": { ... } }</c>.</summary>
internal sealed class UserSearchResponse
{
    public UserDto? User { get; set; }
}

internal sealed class UserDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? SiteUrl { get; set; }
}
