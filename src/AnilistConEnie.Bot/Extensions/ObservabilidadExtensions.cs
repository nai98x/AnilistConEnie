using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Observabilidad;
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
    private static readonly string[] Medidores = ["System.Runtime", "System.Net.Http", "System.Net.NameResolution", "Npgsql"];

    public static bool OtlpHabilitado { get; } =
        !BotEnvironment.EsDebug
        && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

    public static LoggerConfiguration WriteToOtlpSiHabilitado(this LoggerConfiguration loggerConfiguration) =>
        OtlpHabilitado ? loggerConfiguration.WriteTo.OpenTelemetry() : loggerConfiguration;

    public static IServiceCollection AddObservabilidad(this IServiceCollection services)
    {
        if (!OtlpHabilitado)
            return services;

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(FuentesTrazas)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddMeter(Medidores)
                .AddOtlpExporter());

        return services;
    }
}
