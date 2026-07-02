using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Application.Xp;

/// <summary>
/// Fuente única de verdad de los umbrales de XP por rango. Toda la progresión de rangos del servidor
/// (rango actual, siguiente, anterior y XP requerida) se deriva de esta tabla ordenada.
/// </summary>
public static class RangoXp
{
    private static readonly (RangoEnum Rango, long Xp)[] Umbrales =
    [
        (RangoEnum.Miembro, 0),
        (RangoEnum.Tama, 2000),
        (RangoEnum.Casual, 10000),
        (RangoEnum.Kouhai, 40000),
        (RangoEnum.Senpai, 80000),
        (RangoEnum.Hikikomori, 120000),
        (RangoEnum.Sensei, 200000),
        (RangoEnum.Ousama, 500000),
        (RangoEnum.Teiou, 1000000),
    ];

    /// <summary>XP a partir de la cual se alcanza el rango indicado.</summary>
    public static long XpRequerida(RangoEnum rango)
    {
        foreach ((RangoEnum r, long xp) in Umbrales)
            if (r == rango) return xp;
        return 0;
    }

    /// <summary>Rango que corresponde a la XP indicada (el de mayor umbral que no la supera).</summary>
    public static RangoEnum RangoActual(long xp)
    {
        RangoEnum actual = RangoEnum.Miembro;
        foreach ((RangoEnum rango, long umbral) in Umbrales)
            if (xp >= umbral) actual = rango;
        return actual;
    }

    /// <summary>Próximo rango a alcanzar (el primer umbral estrictamente mayor a la XP); Teiou si ya se superó por completo.</summary>
    public static RangoEnum RangoSiguiente(long xp)
    {
        foreach ((RangoEnum rango, long umbral) in Umbrales)
            if (umbral > xp) return rango;
        return RangoEnum.Teiou;
    }

    /// <summary>Rango inmediatamente anterior a la XP indicada (el de mayor umbral estrictamente menor); Miembro si no hay.</summary>
    public static RangoEnum RangoAnterior(long xp)
    {
        RangoEnum anterior = RangoEnum.Miembro;
        foreach ((RangoEnum rango, long umbral) in Umbrales)
            if (umbral < xp) anterior = rango;
        return anterior;
    }

    private const long HitoFicticio = 100_000;

    /// <summary>
    /// Meta a usar como tope de la barra de progreso de XP: el umbral del próximo rango, o si ya no
    /// hay siguiente rango (Teiou superado), el próximo hito ficticio de <see cref="HitoFicticio"/> XP
    /// por delante, para que la barra no quede desbordada.
    /// </summary>
    public static long MetaProgreso(long xp)
    {
        long nextRangeXp = XpRequerida(RangoSiguiente(xp));
        return nextRangeXp > xp ? nextRangeXp : (xp / HitoFicticio + 1) * HitoFicticio;
    }
}
