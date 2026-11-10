using System.Diagnostics;

namespace AnilistConEnie.Bot.Observabilidad;

// Medición en curso del comando/evento/tarea que se está ejecutando. La duración que se reporta como
// métrica es el tiempo activo: se descuenta lo que el bot pasa esperando que el usuario toque un botón
// o complete un modal, que no es tiempo de procesamiento.
public sealed class Medicion
{
    private static readonly AsyncLocal<Medicion?> MedicionActual = new();

    private readonly long _inicio = Stopwatch.GetTimestamp();

    private Medicion()
    {
    }

    public static Medicion? Actual => MedicionActual.Value;

    public string Resultado { get; set; } = Telemetria.ResultadoOk;

    public TimeSpan EsperaUsuario { get; private set; }

    public TimeSpan TiempoActivo => Stopwatch.GetElapsedTime(_inicio) - EsperaUsuario;

    public static Medicion Iniciar() => MedicionActual.Value = new Medicion();

    public void SumarEspera(TimeSpan espera) => EsperaUsuario += espera;
}

public static class EsperaUsuarioExtensions
{
    public static async Task<T> EsperandoUsuario<T>(this Task<T> espera)
    {
        long inicio = Stopwatch.GetTimestamp();

        try
        {
            return await espera;
        }
        finally
        {
            Medicion.Actual?.SumarEspera(Stopwatch.GetElapsedTime(inicio));
        }
    }
}
