namespace AnilistConEnie.Application.Anilist;

public static class AnilistActivityUrl
{
    public static bool TryGetActivityId(string? activityUrl, out int activityId)
    {
        activityId = 0;
        if (string.IsNullOrEmpty(activityUrl)) return false;

        string segment = activityUrl.TrimEnd('/').Split('/').Last();
        return int.TryParse(segment, out activityId);
    }
}
