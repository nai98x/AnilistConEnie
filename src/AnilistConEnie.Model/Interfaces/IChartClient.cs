using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces;

/// <summary>
/// Cliente de generación de gráficos. Centraliza el uso del servicio de charts (QuickChart) para
/// el bot. El manejo del cliente HTTP y del endpoint está en la capa Infrastructure.
/// </summary>
public interface IChartClient
{
    /// <summary>Genera el gráfico y devuelve la imagen ya renderizada (para adjuntar como archivo).</summary>
    Task<byte[]> RenderAsync(ChartRequest request, CancellationToken cancellationToken = default);

    /// <summary>Crea el gráfico en el servicio y devuelve una URL corta y estable (para usar en embeds).</summary>
    Task<string> CreateUrlAsync(ChartRequest request, CancellationToken cancellationToken = default);
}
