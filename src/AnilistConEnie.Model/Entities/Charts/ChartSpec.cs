namespace AnilistConEnie.Model.Entities.Charts;

/// <summary>Porción de un gráfico de torta/donut: etiqueta, valor y color (hex).</summary>
public readonly record struct PieSlice(string Label, double Value, string ColorHex);

/// <summary>Serie de un gráfico de líneas: etiqueta, puntos y color (hex).</summary>
public readonly record struct LineSeries(string Label, IReadOnlyList<double> Data, string ColorHex);

/// <summary>
/// Descripción de un gráfico a renderizar. El tamaño en píxeles queda fijo por tipo de gráfico
/// (ver los records concretos); el renderer (Infrastructure) decide cómo dibujar cada uno.
/// </summary>
public abstract record ChartSpec(int Width, int Height);

/// <summary>Barra de progreso horizontal (ej: xp actual hacia el siguiente rango).</summary>
public sealed record BarProgressSpec(double Value, double Max) : ChartSpec(500, 150);

/// <summary>
/// Gráfico de torta/donut a partir de porciones ya calculadas. <paramref name="DonutFraction"/> es
/// el tamaño del agujero central (0 = torta completa, 1 = anillo fino).
/// </summary>
public sealed record DonutChartSpec(IReadOnlyList<PieSlice> Slices, double DonutFraction = 0.55) : ChartSpec(500, 300);

/// <summary>
/// Gráfico de línea simple (con o sin relleno bajo la curva). Si <paramref name="Label"/> no es
/// null se muestra como leyenda (ej: "Boludómetro de Fulano"); si es null no se muestra leyenda.
/// </summary>
public sealed record LineChartSpec(
    IReadOnlyList<double> Values,
    IReadOnlyList<string> XLabels,
    double Min,
    double Max,
    bool Fill,
    bool ShowXAxis = true,
    string? Label = null) : ChartSpec(500, 300);

/// <summary>Gráfico de múltiples líneas superpuestas (ej: comparativa de xp entre usuarios).</summary>
public sealed record MultiLineChartSpec(
    IReadOnlyList<LineSeries> Series,
    IReadOnlyList<string> XLabels,
    double Min,
    double Max) : ChartSpec(1000, 600);

/// <summary>Gauge radial de un único valor porcentual (0-100).</summary>
public sealed record GaugeSpec(double Value) : ChartSpec(500, 300);
