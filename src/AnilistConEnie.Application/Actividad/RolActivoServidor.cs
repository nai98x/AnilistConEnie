namespace AnilistConEnie.Application.Actividad;

public static class RolActivoServidor
{
    /// <summary>
    /// Ventana de <paramref name="dias"/> días completos que termina ayer: el día en curso no cuenta
    /// porque la evaluación corre a la medianoche, cuando hoy todavía no tiene mensajes.
    /// </summary>
    public static (DateOnly Desde, DateOnly Hasta) Ventana(DateOnly hoy, int dias)
    {
        DateOnly hasta = hoy.AddDays(-1);
        return (RetencionMensajes.FechaMinima(hasta, dias), hasta);
    }

    /// <summary>
    /// <paramref name="maximoEnUnCanal"/> es la mayor cantidad de mensajes en un solo canal sin contar general:
    /// hablar casi solo en un canal que no es general no alcanza, pero hacerlo en general sí.
    /// </summary>
    public static bool Corresponde(long mensajes, long maximoEnUnCanal, long diasConMensajes, int masDeMensajes, int minimoFueraDelCanalPrincipal, int minimoDias,
        DateTimeOffset fechaEntrada, DateTimeOffset ahora, int antiguedadMinimaMeses) =>
        mensajes > masDeMensajes && mensajes - maximoEnUnCanal >= minimoFueraDelCanalPrincipal && diasConMensajes >= minimoDias
        && fechaEntrada <= ahora.AddMonths(-antiguedadMinimaMeses);
}
