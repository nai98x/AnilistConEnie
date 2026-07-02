using ScottPlot;

namespace AnilistConEnie.Infrastructure.Charts;

/// <summary>
/// Tema visual centralizado de los gráficos: paleta oscura acorde a los embeds de Discord, tipografía
/// y colores de acento. Único lugar del renderer que conoce tipos de ScottPlot para colores/fuentes.
/// </summary>
internal static class ChartTheme
{
    private const string FontName = "Inter";

    public static readonly Color Background = Color.FromHex("#2B2D31");
    public static readonly Color DataBackground = Color.FromHex("#2B2D31");
    public static readonly Color TextColor = Color.FromHex("#DBDEE1");
    public static readonly Color GridColor = Color.FromHex("#40434B");

    /// <summary>Paleta de acento para series/porciones sin color semántico propio.</summary>
    public static readonly IReadOnlyList<string> Accent =
    [
        "#5865F2", "#EB459E", "#57F287", "#FEE75C", "#ED4245", "#3BA55D"
    ];

    public static string AccentForIndex(int index) => Accent[index % Accent.Count];

    private static readonly bool FontRegistered;

    static ChartTheme()
    {
        string fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Inter.ttf");
        if (!File.Exists(fontPath)) return;

        Fonts.AddFontFile(FontName, fontPath);
        FontRegistered = true;
    }

    /// <summary>Aplica el chrome oscuro (fondo, ejes, grilla, tipografía) a un plot recién creado.</summary>
    public static void Apply(Plot plot)
    {
        plot.FigureBackground.Color = Background;
        plot.DataBackground.Color = DataBackground;

        if (FontRegistered) plot.Font.Set(FontName);

        plot.Axes.Color(TextColor);
        plot.Grid.MajorLineColor = GridColor;

        plot.Legend.FontColor = TextColor;
        plot.Legend.BackgroundColor = Background;
        plot.Legend.OutlineColor = GridColor;
    }
}
