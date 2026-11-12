using System.Diagnostics;
using System.Diagnostics.Metrics;
using AnilistConEnie.Domain.Enum;
using Serilog.Context;

namespace AnilistConEnie.Bot.Observabilidad;

public enum OrigenXp
{
    Mensaje,
    Booster,
    Reparto,
    Admin
}

// Spans raíz de lo que dispara el bot (comandos, eventos de Discord, tareas programadas): las llamadas
// HTTP y las queries que ocurren adentro quedan como hijas en la misma traza, y los logs escritos
// adentro llevan el comando/evento/tarea como propiedad para poder filtrarlos en Loki.
// Las métricas solo llevan etiquetas de cardinalidad acotada (nombres de comando/evento/tarea y
// resultado; los mensajes van por nombre del canal, con los hilos agrupados en su canal padre): cada
// combinación es una serie y el plan de Grafana Cloud limita las series activas.
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

    private static readonly Counter<long> Xp = Medidor.CreateCounter<long>(
        "bot.xp", "{xp}", "XP otorgada o quitada, por origen");

    private static readonly Counter<long> Mensajes = Medidor.CreateCounter<long>(
        "bot.messages", "{message}", "Mensajes recibidos en el servidor");

    private static readonly Counter<long> CambiosRango = Medidor.CreateCounter<long>(
        "bot.rank.changes", "{change}", "Cambios de rango por XP");

    public static void RegistrarXp(long cantidad, OrigenXp origen, XpOperation operacion = XpOperation.Add)
    {
        if (cantidad <= 0)
            return;

        Xp.Add(cantidad,
            new KeyValuePair<string, object?>("source", origen switch
            {
                OrigenXp.Mensaje => "mensaje",
                OrigenXp.Booster => "booster",
                OrigenXp.Reparto => "reparto",
                _ => "admin"
            }),
            new KeyValuePair<string, object?>("operation", operacion == XpOperation.Add ? "add" : "remove"));
    }

    public static void RegistrarMensaje(bool esBot, bool cuentaComoActividad, string canal)
    {
        if (esBot)
        {
            Mensajes.Add(1, new KeyValuePair<string, object?>("kind", "bot"));
            return;
        }

        Mensajes.Add(1,
            new KeyValuePair<string, object?>("kind", cuentaComoActividad ? "activity" : "other"),
            new KeyValuePair<string, object?>("channel", canal));
    }

    public static void RegistrarCambioRango(RangoEnum anterior, RangoEnum nuevo) =>
        CambiosRango.Add(1,
            new KeyValuePair<string, object?>("rank", nuevo.ToString()),
            new KeyValuePair<string, object?>("direction", nuevo > anterior ? "up" : "down"));

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

    public static ObservableGauge<int> CrearEstadoAnilist(Func<EstadoAnilist?> leer) =>
        Medidor.CreateObservableGauge<int>(
            "bot.anilist.up",
            () => leer() is { } estado ? [new Measurement<int>(estado.Disponible ? 1 : 0)] : Array.Empty<Measurement<int>>(),
            description: "Si la API de AniList respondió a la última sonda (1) o no (0)");

    public static ObservableGauge<double> CrearLatenciaAnilist(Func<EstadoAnilist?> leer) =>
        Medidor.CreateObservableGauge<double>(
            "bot.anilist.latency",
            () => leer() is { } estado ? [new Measurement<double>(estado.UltimaLatenciaMs)] : Array.Empty<Measurement<double>>(),
            "ms",
            "Latencia de la última sonda exitosa a la API de AniList");

    public static void RegistrarEspera(Activity? activity, Medicion medicion)
    {
        if (activity is not null && medicion.EsperaUsuario > TimeSpan.Zero)
            activity.SetTag("bot.user_wait.duration", medicion.EsperaUsuario.TotalSeconds);
    }

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
        Medicion medicion = Medicion.Iniciar();

        try
        {
            await trabajo();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            medicion.Resultado = ResultadoError;
            MarcarError(activity, ex);
            throw;
        }
        finally
        {
            RegistrarEspera(activity, medicion);
            duracion.Record(medicion.TiempoActivo.TotalSeconds, etiqueta, new KeyValuePair<string, object?>("outcome", medicion.Resultado));
        }
    }
}
