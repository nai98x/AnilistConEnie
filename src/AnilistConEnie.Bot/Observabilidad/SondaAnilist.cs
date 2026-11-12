using System.Diagnostics;
using AnilistConEnie.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Observabilidad;

public sealed record EstadoAnilist(bool Disponible, double UltimaLatenciaMs);

// Mide la API de AniList con la consulta más liviana que tiene el cliente. La latencia incluye los
// reintentos y las esperas por rate limit del cliente: es lo que efectivamente tarda el bot en obtener
// una respuesta.
public sealed class SondaAnilist(IAnilistClient anilistClient, ILogger<SondaAnilist> logger)
{
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(15);

    private volatile EstadoAnilist? _ultimo;

    public EstadoAnilist? Ultimo => _ultimo;

    public async Task MedirAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(TiempoMaximo);

        long inicio = Stopwatch.GetTimestamp();
        try
        {
            await anilistClient.GetRateLimitAsync(limite.Token);
            _ultimo = new EstadoAnilist(true, Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            if (_ultimo?.Disponible != false)
                logger.LogWarning(ex, "AniList no respondió a la sonda de estado");

            _ultimo = new EstadoAnilist(false, _ultimo?.UltimaLatenciaMs ?? 0);
        }
    }
}
