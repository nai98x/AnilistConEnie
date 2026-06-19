namespace AnilistConEnie.Helpers;

public static class StringHelper
{
    public static string TextAfter(this string value, string search)
    {
        return value[(value.IndexOf(search, StringComparison.Ordinal) + search.Length)..];
    }
}
