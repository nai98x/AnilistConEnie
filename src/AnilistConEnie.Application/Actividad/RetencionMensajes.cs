namespace AnilistConEnie.Application.Actividad;

public static class RetencionMensajes
{
    /// <summary>Primer día que se conserva de una ventana de <paramref name="diasRetencion"/> días que incluye hoy.</summary>
    public static DateOnly FechaMinima(DateOnly hoy, int diasRetencion) => hoy.AddDays(-(diasRetencion - 1));
}
