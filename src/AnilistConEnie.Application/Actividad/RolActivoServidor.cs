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

    public static bool Corresponde(long mensajes, long diasConMensajes, int masDeMensajes, int minimoDias) =>
        mensajes > masDeMensajes && diasConMensajes >= minimoDias;
}
