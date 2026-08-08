using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Application.Xp;

/// <summary>Progreso del usuario hacia su próximo rango.</summary>
public readonly record struct XpProgressionInfo(RangoEnum NextRango, long NextRangeXp, long NextXp, int MensajesNecesarios);

public static class XpProgression
{
    /// <summary>XP promedio que otorga un mensaje (usado para estimar cuántos mensajes faltan para el próximo rango).</summary>
    private const int XpPorMensaje = 15;

    public static XpProgressionInfo Describe(long total, double promedioDiario)
    {
        RangoEnum nextRango = RangoXp.RangoSiguiente(total);
        long nextRangeXp = RangoXp.XpRequerida(nextRango);
        long nextXp = nextRangeXp - total;
        int mensajesNecesarios = Math.Max(0, (int)Math.Ceiling((nextXp - promedioDiario) / XpPorMensaje));

        return new XpProgressionInfo(nextRango, nextRangeXp, nextXp, mensajesNecesarios);
    }

    /// <summary>Indica si al usuario le falta poca XP para alcanzar su próximo rango (umbral por rango).</summary>
    public static bool EstaProximoASubir(RangoEnum nextRango, long nextXp) => nextRango switch
    {
        RangoEnum.Tama => nextXp < 500,
        RangoEnum.Casual => nextXp < 1000,
        RangoEnum.Kouhai or RangoEnum.Senpai => nextXp < 2500,
        RangoEnum.Hikikomori or RangoEnum.Sensei => nextXp < 5000,
        RangoEnum.Ousama => nextXp < 7500,
        RangoEnum.Teiou => nextXp < 10000,
        _ => false
    };

    public static string EstimarTiempo(long nextXp, double promedioXp, string rangoName, int mensajes)
    {
        if (promedioXp <= 0)
            return $"- No se puede estimar el tiempo para el siguiente rango ({rangoName}) porque no se ha registrado ningún punto de experiencia ganado.";

        if (nextXp <= 0)
            return $"- ¡Felicidades! Ya has alcanzado el rango **{rangoName}** o lo has superado.";

        int diasTotales = (int)Math.Ceiling(nextXp / promedioXp);

        if (diasTotales < 365)
            return $"- A este ritmo llegarás a **{rangoName}** en **{diasTotales} días** (equivale a aproximadamente {mensajes.ToSpanish()} mensajes)";

        int anios = diasTotales / 365;
        int diasRestantes = diasTotales % 365;

        return diasRestantes == 0
            ? $"- A este ritmo llegarás a **{rangoName}** en **{anios} año(s)** (equivale a aproximadamente {mensajes.ToSpanish()} mensajes)"
            : $"- A este ritmo llegarás a **{rangoName}** en **{anios} año(s) y {diasRestantes} días** (equivale a aproximadamente {mensajes.ToSpanish()} mensajes)";
    }
}
