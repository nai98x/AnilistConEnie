namespace AnilistConEnie.Application.Helpers;

public static class NumberHelper
{
    public static int GetNumeroRandom(int min, int max)
    {
        if (min <= 0 && max <= 0)
            return 0;
        Random rnd = new();
        return rnd.Next(minValue: min, maxValue: max);
    }
}
