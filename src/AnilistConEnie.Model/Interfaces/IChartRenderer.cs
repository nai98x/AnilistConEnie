using AnilistConEnie.Model.Entities.Charts;

namespace AnilistConEnie.Model.Interfaces;

/// <summary>
/// Renderer de gráficos. Recibe un <see cref="ChartSpec"/> ya armado con los datos y devuelve la
/// imagen PNG generada, para adjuntar como archivo al mensaje de Discord.
/// </summary>
public interface IChartRenderer
{
    Task<byte[]> RenderAsync(ChartSpec spec, CancellationToken cancellationToken = default);
}
