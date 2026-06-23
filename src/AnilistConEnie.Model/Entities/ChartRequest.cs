namespace AnilistConEnie.Model.Entities;

/// <summary>
/// Descripción de un gráfico a generar. <see cref="Config"/> es la configuración de Chart.js
/// (puede incluir funciones JS), tal como la acepta QuickChart.
/// </summary>
public sealed record ChartRequest
{
    public required string Config { get; init; }
    public int Width { get; init; } = 500;
    public int Height { get; init; } = 300;
    public string? BackgroundColor { get; init; }
    public string Format { get; init; } = "png";
}
