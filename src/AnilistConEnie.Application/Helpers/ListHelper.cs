namespace AnilistConEnie.Application.Helpers;

public static class ListHelper
{
    private static readonly Random Rng = new();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}
