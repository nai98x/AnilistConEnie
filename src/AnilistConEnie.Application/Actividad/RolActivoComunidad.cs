namespace AnilistConEnie.Application.Actividad;

public static class RolActivoComunidad
{
    /// <summary>Ventana de <paramref name="dias"/> días que termina hoy, con el día en curso parcial.</summary>
    public static (DateOnly Desde, DateOnly Hasta) Ventana(DateOnly hoy, int dias) => (RetencionMensajes.FechaMinima(hoy, dias), hoy);

    /// <summary>
    /// <paramref name="maximoEnUnCanal"/> es la mayor cantidad de mensajes en un solo canal sin contar general:
    /// hablar casi solo en un canal que no es general no alcanza, pero hacerlo en general sí.
    /// </summary>
    public static bool Corresponde(long mensajes, long maximoEnUnCanal, long diasConMensajes, int masDeMensajes, int minimoFueraDelCanalPrincipal, int minimoDias,
        DateTimeOffset fechaEntrada, DateTimeOffset ahora, int antiguedadMinimaMeses) =>
        Evaluar(mensajes, maximoEnUnCanal, diasConMensajes, masDeMensajes, minimoFueraDelCanalPrincipal, minimoDias, fechaEntrada, ahora, antiguedadMinimaMeses).Corresponde;

    public static EvaluacionRolActivo Evaluar(long mensajes, long maximoEnUnCanal, long diasConMensajes, int masDeMensajes, int minimoFueraDelCanalPrincipal, int minimoDias,
        DateTimeOffset fechaEntrada, DateTimeOffset ahora, int antiguedadMinimaMeses) =>
        new(
            Mensajes: mensajes > masDeMensajes,
            FueraDelCanalPrincipal: mensajes - maximoEnUnCanal >= minimoFueraDelCanalPrincipal,
            Dias: diasConMensajes >= minimoDias,
            Antiguedad: fechaEntrada <= ahora.AddMonths(-antiguedadMinimaMeses));
}

public sealed record EvaluacionRolActivo(bool Mensajes, bool FueraDelCanalPrincipal, bool Dias, bool Antiguedad)
{
    public bool Corresponde => Mensajes && FueraDelCanalPrincipal && Dias && Antiguedad;
}
