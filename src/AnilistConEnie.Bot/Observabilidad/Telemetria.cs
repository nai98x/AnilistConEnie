using System.Diagnostics;
using System.Diagnostics.Metrics;
using Serilog.Context;

namespace AnilistConEnie.Bot.Observabilidad;

// Spans raíz de lo que dispara el bot (comandos, eventos de Discord, tareas programadas): las llamadas
// HTTP y las queries que ocurren adentro quedan como hijas en la misma traza, y los logs escritos
// adentro llevan el comando/evento/tarea como propiedad para poder filtrarlos en Loki.
// Las métricas solo llevan etiquetas de cardinalidad acotada (nombres de comando/evento/tarea y
// resultado): cada combinación es una serie y el plan de Grafana Cloud limita las series activas.
public static class Telemetria
{
    public const string NombreFuente = "AnilistConEnie";

    public const string ResultadoOk = "ok";
    public const string ResultadoError = "error";
    public const string ResultadoChecksFallidos = "checks_failed";

    public static readonly ActivitySource Fuente = new(NombreFuente);

    private static readonly Meter Medidor = new(NombreFuente);

    private static readonly InstrumentAdvice<double> BucketsDuracion = new()
    {
        HistogramBucketBoundaries = [0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10]
    };

    public static readonly Histogram<double> DuracionComandos = Medidor.CreateHistogram(
        "bot.command.duration", "s", "Duración de la ejecución de comandos", advice: BucketsDuracion);

    private static readonly Histogram<double> DuracionEventos = Medidor.CreateHistogram(
        "bot.event.duration", "s", "Duración del manejo de eventos de Discord", advice: BucketsDuracion);

    private static readonly Histogram<double> DuracionTareas = Medidor.CreateHistogram(
        "bot.task.duration", "s", "Duración de las tareas programadas", advice: BucketsDuracion);

    public static Task TrazarEventoAsync(string evento, Func<Task> trabajo) =>
        TrazarAsync($"evento {evento}", DuracionEventos, new KeyValuePair<string, object?>("event", evento), trabajo);

    public static Task TrazarTareaAsync(string tarea, Func<Task> trabajo) =>
        TrazarAsync($"tarea {tarea}", DuracionTareas, new KeyValuePair<string, object?>("task", tarea), trabajo);

    public static ObservableGauge<double> CrearLatenciaGateway(Func<TimeSpan> medir) =>
        Medidor.CreateObservableGauge<double>(
            "bot.gateway.latency",
            () => medir() is { TotalMilliseconds: > 0 } ping ? [new Measurement<double>(ping.TotalMilliseconds)] : Array.Empty<Measurement<double>>(),
            "ms",
            "Latencia del heartbeat con el gateway de Discord");

    public static void MarcarError(Activity? activity, Exception ex)
    {
        if (activity is null)
            return;

        activity.AddException(ex);
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
    }

    private static async Task TrazarAsync(string nombreSpan, Histogram<double> duracion, KeyValuePair<string, object?> etiqueta, Func<Task> trabajo)
    {
        using Activity? activity = Fuente.StartActivity(nombreSpan);
        using IDisposable contextoLogs = LogContext.PushProperty(etiqueta.Key, etiqueta.Value);
        long inicio = Stopwatch.GetTimestamp();
        string resultado = ResultadoOk;

        try
        {
            await trabajo();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            resultado = ResultadoError;
            MarcarError(activity, ex);
            throw;
        }
        finally
        {
            duracion.Record(Stopwatch.GetElapsedTime(inicio).TotalSeconds, etiqueta, new KeyValuePair<string, object?>("outcome", resultado));
        }
    }
}
