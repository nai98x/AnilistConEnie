namespace AnilistConEnie.Application.Anilist;

public static class AnilistProfileUrl
{
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
