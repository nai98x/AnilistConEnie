namespace AnilistConEnie.Application.Anilist;

public static class AnilistProfileUrl
{
    /// <summary>
    /// Extrae el nombre de usuario de una URL de perfil (<c>https://anilist.co/user/Nombre</c>) o,
    /// si la entrada no es una URL, la devuelve tal cual recortada. Sirve para buscar por nombre.
    /// </summary>
    public static string ExtractUserName(string input)
    {
        string trimmed = input.Trim().TrimEnd('/');
        int slash = trimmed.LastIndexOf('/');
        return slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
    }

    public static bool TryGetUserId(string? profileUrl, out int userId)
    {
        userId = 0;
        if (string.IsNullOrEmpty(profileUrl)) return false;

        string trimmed = profileUrl.TrimEnd('/');
        int slash = trimmed.LastIndexOf('/');
        string segment = slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
        return int.TryParse(segment, out userId);
    }
}
