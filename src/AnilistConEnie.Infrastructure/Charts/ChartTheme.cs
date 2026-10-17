using System.Globalization;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace AnilistConEnie.Infrastructure.Charts;

/// <summary>
/// Tema visual centralizado de los gráficos: paleta oscura acorde a los embeds de Discord, tipografía
/// y colores de acento. Único lugar del renderer que conoce tipos de ScottPlot para colores/fuentes.
/// </summary>
internal static class ChartTheme
{
    public const string FontName = "Inter";

    /// <summary>
    /// Multiplicador de resolución: los specs definen el tamaño lógico y la imagen sale al doble de
    /// píxeles, así Discord la muestra nítida en pantallas de alta densidad y al abrirla.
    /// </summary>
    public const float Scale = 2f;

    public static readonly Color Background = Color.FromHex("#2B2D31");
    public static readonly Color DataBackground = Color.FromHex("#2B2D31");
    public static readonly Color TextColor = Color.FromHex("#DBDEE1");
    public static readonly Color MutedTextColor = Color.FromHex("#949BA4");
    public static readonly Color GridColor = Color.FromHex("#3A3C42");
    public static readonly Color BaselineColor = Color.FromHex("#4E5058");
    public static readonly Color TrackColor = Color.FromHex("#3F4147");

    /// <summary>
    /// Colores de estado (bien / atención / crítico), reservados para indicadores como el gauge y
    /// distintos de la paleta de series para que no se confundan con una categoría.
    /// </summary>
    public static readonly Color StatusGood = Color.FromHex("#0CA30C");
    public static readonly Color StatusWarning = Color.FromHex("#FAB219");
    public static readonly Color StatusCritical = Color.FromHex("#D03B3B");

    public static readonly string? FontPath;

    private static readonly CultureInfo SpanishFormat = CultureInfo.GetCultureInfo("es-AR");

    static ChartTheme()
    {
        string fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter.ttf");
        if (!File.Exists(fontPath)) return;

        Fonts.AddFontFile(FontName, fontPath);
        FontPath = fontPath;
    }

    /// <summary>
    /// Aplica el chrome oscuro (fondo, ejes, grilla, tipografía) a un plot recién creado: solo la
    /// línea base inferior y la grilla horizontal, sin marcas de ticks ni recuadro.
    /// </summary>
    public static void Apply(Plot plot)
    {
        plot.ScaleFactor = Scale;
        plot.FigureBackground.Color = Background;
        plot.DataBackground.Color = DataBackground;

        if (FontPath is not null) plot.Font.Set(FontName);

        plot.Axes.Color(MutedTextColor);
        plot.Axes.Frame(false);
        plot.Axes.Bottom.FrameLineStyle.IsVisible = true;
        plot.Axes.Bottom.FrameLineStyle.Color = BaselineColor;

        foreach (IAxis axis in plot.Axes.GetAxes())
        {
            axis.MajorTickStyle.Length = 0;
            axis.MinorTickStyle.Length = 0;
            axis.TickLabelStyle.FontSize = 12;
        }

        plot.Grid.XAxisStyle.IsVisible = false;
        plot.Grid.YAxisStyle.MajorLineStyle.Color = GridColor;
        plot.Grid.YAxisStyle.MajorLineStyle.Width = 1;

        plot.Axes.Title.Label.ForeColor = TextColor;
        plot.Axes.Title.Label.FontSize = 15;
        plot.Axes.Title.Label.Bold = false;

        plot.Legend.FontColor = TextColor;
        plot.Legend.FontSize = 13;
        plot.Legend.BackgroundColor = Colors.Transparent;
        plot.Legend.OutlineColor = Colors.Transparent;
        plot.Legend.ShadowColor = Colors.Transparent;
    }

    /// <summary>Ticks del eje Y con números compactos ("12,5k", "1,2M") para no ensanchar el margen izquierdo.</summary>
    public static void CompactYTicks(Plot plot) =>
        plot.Axes.Left.TickGenerator = new NumericAutomatic { LabelFormatter = Compact, MaxTickCount = 6 };

    public static string Compact(double value) => Math.Abs(value) switch
    {
        >= 1_000_000 => (value / 1_000_000).ToString("0.#", SpanishFormat) + "M",
        >= 1_000 => (value / 1_000).ToString("0.#", SpanishFormat) + "k",
        _ => value.ToString("0.#", SpanishFormat)
    };
}
