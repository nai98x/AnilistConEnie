using System.Globalization;

namespace AnilistConEnie.Application.Helpers;

public static class NumberHelper
{
    /// <summary>Formato numérico es-ES (separador de miles ".", sin decimales) usado en los rankings de XP.</summary>
    public static readonly NumberFormatInfo SpanishFormat = new CultureInfo("es-ES", false).NumberFormat;

    static NumberHelper()
    {
        SpanishFormat.NumberDecimalSeparator = ",";
        SpanishFormat.NumberGroupSeparator = ".";
        SpanishFormat.NumberDecimalDigits = 0;
    }

    public static string ToSpanish(this int value) => value.ToString("N", SpanishFormat);

    public static string ToSpanish(this long value) => value.ToString("N", SpanishFormat);

    public static string ToSpanish(this double value) => value.ToString("N", SpanishFormat);

    public static int GetNumeroRandom(int min, int max)
    {
        if (min <= 0 && max <= 0)
            return 0;
        Random rnd = new();
        return rnd.Next(minValue: min, maxValue: max);
    }

    public static long ObtenerMultiploAnterior(long numero, int multiplo) => numero / multiplo * multiplo;

    public static long ObtenerMultiploSiguiente(long numero, int multiplo) => (numero + (multiplo - 1)) / multiplo * multiplo;
}
