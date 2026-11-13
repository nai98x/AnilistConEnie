using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Observabilidad;
using DSharpPlus;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

namespace AnilistConEnie.Bot.Extensions;

// Solo en el bot desplegado (release). El destino (endpoint, protocolo, headers de auth, service.name y
// atributos del resource) se toma de las variables de entorno estándar OTEL_*; sin
// OTEL_EXPORTER_OTLP_ENDPOINT no se exporta nada.
public static class ObservabilidadExtensions
{
    private static readonly string[] FuentesTrazas = [Telemetria.NombreFuente, "System.Net.Http", "Npgsql"];
    private static readonly string[] Medidores = [Telemetria.NombreFuente, "System.Runtime", "System.Net.Http", "Npgsql"];

    // Instrumentos de librerías que no se consultan en el dashboard ni en alertas: solo consumen series activas.
    private static readonly HashSet<string> InstrumentosDescartados =
    [
        "http.client.connection.duration",
        "http.client.request.time_in_queue",
        "http.client.open_connections",
        "db.client.connection.npgsql.create_time"
    ];

    private static readonly string[] EtiquetasHttp = ["server.address", "http.request.method", "http.response.status_code", "error.type"];

    public static bool OtlpHabilitado { get; } =
        !BotEnvironment.EsDebug
        && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

    public static LoggerConfiguration WriteToOtlpSiHabilitado(this LoggerConfiguration loggerConfiguration) =>
        OtlpHabilitado ? loggerConfiguration.WriteTo.OpenTelemetry() : loggerConfiguration;

    public static IServiceCollection AddObservabilidad(this IServiceCollection services)
    {
        services.AddSingleton<SondaAnilist>();

        if (!OtlpHabilitado)
            return services;

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(FuentesTrazas)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddMeter(Medidores)
                .AddView(ConfigurarInstrumento)
                .AddInstrumentation(sp =>
                {
                    DiscordClient client = sp.GetRequiredService<DiscordClient>();
                    ulong guildId = sp.GetRequiredService<BotConfiguration>().GuildId;
                    return Telemetria.CrearLatenciaGateway(() => client.GetConnectionLatency(guildId));
                })
                .AddInstrumentation(sp =>
                {
                    SondaAnilist sonda = sp.GetRequiredService<SondaAnilist>();
                    return new object[]
                    {
                        Telemetria.CrearEstadoAnilist(() => sonda.Ultimo),
                        Telemetria.CrearLatenciaAnilist(() => sonda.Ultimo)
                    };
                })
                .AddOtlpExporter());

        return services;
    }

    private static MetricStreamConfiguration? ConfigurarInstrumento(Instrument instrumento)
    {
        if (InstrumentosDescartados.Contains(instrumento.Name))
            return MetricStreamConfiguration.Drop;

        if (instrumento.Name != "http.client.request.duration")
            return null;

        return new ExplicitBucketHistogramConfiguration
        {
            Boundaries = Telemetria.LimitesDuracion,
            TagKeys = EtiquetasHttp
        };
    }
}
