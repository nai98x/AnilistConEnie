using AnilistConEnie.Application.Charts;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Domain.Interfaces;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using SkiaSharp;
using ChartSpecs = AnilistConEnie.Domain.Entities.Charts;

namespace AnilistConEnie.Infrastructure.Charts;

/// <summary>
/// Renderer local de gráficos con ScottPlot/SkiaSharp. El tema visual (colores, fuente) vive en
/// <see cref="ChartTheme"/>.
/// </summary>
internal sealed class ScottPlotChartRenderer : IChartRenderer
{
    private const float LineWidth = 2.5f;

    public Task<byte[]> RenderAsync(ChartSpecs.ChartSpec spec, CancellationToken cancellationToken = default) =>
        // El render es CPU-bound (rasterizado con Skia); lo corremos fuera del hilo del gateway de Discord.
        Task.Run(() => Render(spec), cancellationToken);

    private static byte[] Render(ChartSpecs.ChartSpec spec)
    {
        switch (spec)
        {
            case ChartSpecs.BarProgressSpec progress: return RenderBarProgress(progress);
            case ChartSpecs.GaugeSpec gauge: return RenderGauge(gauge);
        }

        Plot plot = new();
        ChartTheme.Apply(plot);

        switch (spec)
        {
            case ChartSpecs.DonutChartSpec donut: RenderDonut(plot, donut); break;
            case ChartSpecs.LineChartSpec line: RenderLine(plot, line); break;
            case ChartSpecs.MultiLineChartSpec multi: RenderMultiLine(plot, multi); break;
            default: throw new NotSupportedException($"Tipo de gráfico no soportado: {spec.GetType().Name}");
        }

        return plot.GetImageBytes(Scaled(spec.Width), Scaled(spec.Height), ImageFormat.Png);
    }

    private static int Scaled(int size) => (int)(size * ChartTheme.Scale);

    private static byte[] RenderBarProgress(ChartSpecs.BarProgressSpec spec)
    {
        // Se dibuja directo con Skia: una barra de progreso no necesita ejes, y así queda con track
        // redondeado y el valor/porcentaje como encabezado en lugar de ticks numéricos.
        double max = spec.Max > 0 ? spec.Max : 1;
        double fraction = Math.Clamp(spec.Value / max, 0, 1);

        using SKSurface surface = CreateSurface(spec.Width, spec.Height);
        SKCanvas canvas = surface.Canvas;

        using SKTypeface typeface = LoadTypeface();
        using SKFont valueFont = new(typeface, 30) { Embolden = true };
        using SKFont maxFont = new(typeface, 18);
        using SKFont percentFont = new(typeface, 22) { Embolden = true };
        using SKPaint textPaint = new() { Color = ToSk(ChartTheme.TextColor), IsAntialias = true };
        using SKPaint mutedPaint = new() { Color = ToSk(ChartTheme.MutedTextColor), IsAntialias = true };

        const float padding = 24;
        const float baseline = 70;
        float right = spec.Width - padding;

        string valueText = spec.Value.ToSpanish();
        canvas.DrawText(valueText, padding, baseline, SKTextAlign.Left, valueFont, textPaint);
        float valueWidth = valueFont.MeasureText(valueText);
        canvas.DrawText($" / {max.ToSpanish()}", padding + valueWidth, baseline, SKTextAlign.Left, maxFont, mutedPaint);
        canvas.DrawText($"{Math.Floor(fraction * 100):0}%", right, baseline, SKTextAlign.Right, percentFont, textPaint);

        const float trackTop = 90;
        const float trackHeight = 22;
        const float radius = trackHeight / 2;
        SKRect track = new(padding, trackTop, right, trackTop + trackHeight);

        using SKPaint trackPaint = new() { Color = ToSk(ChartTheme.TrackColor), IsAntialias = true };
        canvas.DrawRoundRect(track, radius, radius, trackPaint);

        if (fraction > 0)
        {
            // Mínimo del ancho de la barra = su alto, para que con valores muy chicos siga viéndose
            // como una píldora y no como un óvalo deformado.
            float fillWidth = Math.Max(trackHeight, (float)(track.Width * fraction));
            SKRect fill = new(track.Left, track.Top, track.Left + fillWidth, track.Bottom);
            using SKPaint fillPaint = new() { Color = SKColor.Parse(ChartColors.ForIndex(0)), IsAntialias = true };
            canvas.DrawRoundRect(fill, radius, radius, fillPaint);
        }

        return EncodePng(surface);
    }

    private static void RenderDonut(Plot plot, ChartSpecs.DonutChartSpec spec)
    {
        // Sin texto sobre las porciones: con categorías chicas/parejas los labels se superponen.
        // El nombre y el porcentaje de cada una van en la leyenda.
        double total = spec.Slices.Sum(s => s.Value);
        List<PieSlice> slices =
        [
            .. spec.Slices.Select(s => new PieSlice
            {
                Value = s.Value,
                FillColor = Color.FromHex(s.ColorHex),
                LegendText = total > 0 ? $"{s.Label}  {s.Value / total * 100:0.#}%" : s.Label
            })
        ];

        Pie pie = plot.Add.Pie(slices);
        pie.DonutFraction = spec.DonutFraction;
        // Separación entre porciones en el color del fondo, en vez de un borde oscuro alrededor.
        pie.LineColor = ChartTheme.Background;
        pie.LineWidth = 2;

        plot.Axes.Frameless();
        plot.HideGrid();
        plot.ShowLegend(Edge.Right);
    }

    private static void RenderLine(Plot plot, ChartSpecs.LineChartSpec spec)
    {
        double[] xs = [.. Enumerable.Range(0, spec.Values.Count).Select(i => (double)i)];
        double[] ys = [.. spec.Values];
        Color accent = Color.FromHex(ChartColors.ForIndex(0));

        Scatter scatter = AddLine(plot, xs, ys, accent);

        if (spec.Fill)
        {
            scatter.FillY = true;
            scatter.FillYValue = spec.Min;
            scatter.FillYAboveColor = accent.WithAlpha(.18);
        }

        // Una sola serie: el nombre va como título y no hace falta una caja de leyenda.
        if (spec.Label is not null) plot.Title(spec.Label);

        if (ys.Length > 0) AddEndDot(plot, xs[^1], ys[^1], accent);

        plot.Axes.SetLimitsY(spec.Min, spec.Max);
        ChartTheme.CompactYTicks(plot);
        ApplyXLabels(plot, spec.XLabels, spec.Width, spec.ShowXAxis);
    }

    private static void RenderMultiLine(Plot plot, ChartSpecs.MultiLineChartSpec spec)
    {
        double[] xs = [.. Enumerable.Range(0, spec.XLabels.Count).Select(i => (double)i)];

        foreach (ChartSpecs.LineSeries series in spec.Series)
        {
            Color color = Color.FromHex(series.ColorHex);
            Scatter scatter = AddLine(plot, xs, [.. series.Data], color);
            scatter.LegendText = series.Label;

            if (series.Data.Count > 0) AddEndDot(plot, series.Data.Count - 1, series.Data[^1], color);
        }

        plot.Axes.SetLimitsY(spec.Min, spec.Max);
        ChartTheme.CompactYTicks(plot);
        ApplyXLabels(plot, spec.XLabels, spec.Width, showAxis: true);
        plot.ShowLegend(Edge.Right);
    }

    private static byte[] RenderGauge(ChartSpecs.GaugeSpec spec)
    {
        // Semicírculo estilo velocímetro dibujado con Skia: track completo de 180° y encima el arco
        // del valor, arrancando a la izquierda y barriendo por arriba hacia la derecha.
        double value = Math.Clamp(spec.Value, 0, 100);

        const float radius = 110;
        const float stroke = 26;
        const float top = 28;
        float centerX = spec.Width / 2f;
        float centerY = top + stroke / 2 + radius;
        int height = (int)(centerY + stroke / 2 + 24);

        using SKSurface surface = CreateSurface(spec.Width, height);
        SKCanvas canvas = surface.Canvas;

        SKRect arcBounds = new(centerX - radius, centerY - radius, centerX + radius, centerY + radius);
        using SKPaint trackPaint = new()
        {
            Color = ToSk(ChartTheme.TrackColor), IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = stroke, StrokeCap = SKStrokeCap.Round
        };
        canvas.DrawArc(arcBounds, 180, 180, false, trackPaint);

        if (value > 0)
        {
            using SKPaint valuePaint = new()
            {
                Color = ToSk(GaugeColor(value)), IsAntialias = true, Style = SKPaintStyle.Stroke,
                StrokeWidth = stroke, StrokeCap = SKStrokeCap.Round
            };
            canvas.DrawArc(arcBounds, 180, (float)(180 * value / 100), false, valuePaint);
        }

        using SKTypeface typeface = LoadTypeface();
        using SKFont font = new(typeface, 44) { Embolden = true };
        using SKPaint textPaint = new() { Color = ToSk(ChartTheme.TextColor), IsAntialias = true };
        canvas.DrawText($"{value:0}%", centerX, centerY - 4, SKTextAlign.Center, font, textPaint);

        return EncodePng(surface);
    }

    private static Color GaugeColor(double value) => value switch
    {
        >= 75 => ChartTheme.StatusCritical,
        >= 40 => ChartTheme.StatusWarning,
        _ => ChartTheme.StatusGood
    };

    private static Scatter AddLine(Plot plot, double[] xs, double[] ys, Color color)
    {
        Scatter scatter = plot.Add.Scatter(xs, ys);
        scatter.Color = color;
        scatter.LineWidth = LineWidth;
        scatter.LineStyle.Rounded = true;
        scatter.MarkerSize = 0;
        return scatter;
    }

    /// <summary>Punto al final de la serie con un anillo del color de fondo, para que se lea sobre otras líneas.</summary>
    private static void AddEndDot(Plot plot, double x, double y, Color color)
    {
        plot.Add.Marker(x, y, MarkerShape.FilledCircle, 13, ChartTheme.Background);
        plot.Add.Marker(x, y, MarkerShape.FilledCircle, 9, color);
    }

    private static void ApplyXLabels(Plot plot, IReadOnlyList<string> labels, int width, bool showAxis)
    {
        int count = labels.Count;
        int longest = showAxis && count > 0 ? labels.Max(l => l.Length) : 0;

        // Margen horizontal suficiente para que la primera/última etiqueta (centradas sobre su punto)
        // no se corten contra el borde. Ancho aproximado del área de datos descontando el eje Y.
        double halfLabel = longest * 3.4 + 4;
        double dataWidth = Math.Max(1, width - 60 - 2 * halfLabel);
        double padding = Math.Max(.3, halfLabel * Math.Max(1, count - 1) / dataWidth);
        plot.Axes.SetLimitsX(-padding, Math.Max(0, count - 1) + padding);

        if (!showAxis || count == 0)
        {
            plot.Axes.Bottom.TickLabelStyle.IsVisible = false;
            return;
        }

        // Etiquetas horizontales y salteadas a intervalos regulares según cuántas entran a lo ancho,
        // en vez de una por punto rotada (con 30+ fechas quedaban encimadas).
        int maxTicks = Math.Max(2, (int)(width / (longest * 7.5 + 28)));
        int step = count <= maxTicks ? 1 : (int)Math.Ceiling((count - 1) / (double)(maxTicks - 1));

        List<int> indices = [.. Enumerable.Range(0, count).Where(i => i % step == 0)];
        int last = count - 1;
        // La última etiqueta se muestra siempre; si quedaría pegada a la anterior, la reemplaza.
        if (indices[^1] != last)
        {
            if (indices.Count > 1 && last - indices[^1] < step * .6) indices[^1] = last;
            else indices.Add(last);
        }

        plot.Axes.Bottom.TickGenerator = new NumericManual([.. indices.Select(i => new Tick(i, labels[i]))]);
    }

    private static SKSurface CreateSurface(int width, int height)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(Scaled(width), Scaled(height)));
        surface.Canvas.Scale(ChartTheme.Scale);
        surface.Canvas.Clear(ToSk(ChartTheme.Background));
        return surface;
    }

    private static SKTypeface LoadTypeface() =>
        ChartTheme.FontPath is not null ? SKTypeface.FromFile(ChartTheme.FontPath) : SKTypeface.Default;

    private static byte[] EncodePng(SKSurface surface)
    {
        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKColor ToSk(Color color) => new(color.R, color.G, color.B, color.A);
}
