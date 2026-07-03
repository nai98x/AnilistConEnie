using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Model.Interfaces;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using SkiaSharp;
using ChartSpecs = AnilistConEnie.Model.Entities.Charts;

namespace AnilistConEnie.Infrastructure.Charts;

/// <summary>
/// Renderer local de gráficos con ScottPlot/SkiaSharp. El tema visual (colores, fuente) vive en
/// <see cref="ChartTheme"/>.
/// </summary>
internal sealed class ScottPlotChartRenderer : IChartRenderer
{
    public Task<byte[]> RenderAsync(ChartSpecs.ChartSpec spec, CancellationToken cancellationToken = default) =>
        // El render es CPU-bound (rasterizado con Skia); lo corremos fuera del hilo del gateway de Discord.
        Task.Run(() => Render(spec), cancellationToken);

    private static byte[] Render(ChartSpecs.ChartSpec spec)
    {
        Plot plot = new();
        ChartTheme.Apply(plot);

        switch (spec)
        {
            case ChartSpecs.BarProgressSpec bar: RenderBarProgress(plot, bar); break;
            case ChartSpecs.DonutChartSpec donut: RenderDonut(plot, donut); break;
            case ChartSpecs.LineChartSpec line: RenderLine(plot, line); break;
            case ChartSpecs.MultiLineChartSpec multi: RenderMultiLine(plot, multi); break;
            case ChartSpecs.GaugeSpec gauge: return RenderGauge(plot, gauge);
            default: throw new NotSupportedException($"Tipo de gráfico no soportado: {spec.GetType().Name}");
        }

        return plot.GetImageBytes(spec.Width, spec.Height, ImageFormat.Png);
    }

    private static void RenderBarProgress(Plot plot, ChartSpecs.BarProgressSpec spec)
    {
        double max = spec.Max > 0 ? spec.Max : 1;

        BarPlot bars = plot.Add.Bars([spec.Value]);
        bars.Horizontal = true;
        bars.Bars[0].FillColor = Color.FromHex(ChartTheme.AccentForIndex(0));
        bars.Bars[0].Label = spec.Value.ToSpanish();
        // Pasado ~85% ya no entra el label a la derecha de la barra (se corta contra el borde), así
        // que ahí se pasa a centrado adentro; por debajo queda afuera como siempre.
        bars.Bars[0].CenterLabel = spec.Value >= max * 0.85;
        bars.ValueLabelStyle.Bold = true;
        bars.ValueLabelStyle.FontSize = 22;
        bars.ValueLabelStyle.ForeColor = ChartTheme.TextColor;

        // Un poco de margen a los costados: si el eje termina justo en 0/max, esos ticks quedan
        // centrados sobre el borde del canvas y se cortan a la mitad.
        plot.Axes.SetLimitsX(-max * 0.01, max * 1.09);
        plot.Axes.Left.IsVisible = false;
        plot.HideGrid();
    }

    private static void RenderDonut(Plot plot, ChartSpecs.DonutChartSpec spec)
    {
        // Sin texto sobre las porciones: con categorías chicas/parejas los labels se superponen.
        // El nombre de cada una ya se muestra en la leyenda.
        List<PieSlice> slices =
        [
            .. spec.Slices.Select(s => new PieSlice
            {
                Value = s.Value,
                FillColor = Color.FromHex(s.ColorHex),
                LegendText = s.Label
            })
        ];

        Pie pie = plot.Add.Pie(slices);
        pie.DonutFraction = spec.DonutFraction;

        plot.Axes.Frameless();
        plot.HideGrid();
        plot.ShowLegend();
    }

    private static void RenderLine(Plot plot, ChartSpecs.LineChartSpec spec)
    {
        double[] xs = [.. Enumerable.Range(0, spec.Values.Count).Select(i => (double)i)];
        double[] ys = [.. spec.Values];
        string accent = ChartTheme.AccentForIndex(0);

        Scatter scatter = plot.Add.Scatter(xs, ys);
        scatter.Color = Color.FromHex(accent);
        scatter.LineWidth = 3;
        scatter.MarkerSize = 0;

        if (spec.Fill)
        {
            scatter.FillY = true;
            scatter.FillYValue = spec.Min;
            scatter.FillYAboveColor = Color.FromHex(accent).WithAlpha(.25);
        }

        if (spec.Label is not null)
        {
            scatter.LegendText = spec.Label;
            plot.ShowLegend();
        }

        plot.Axes.SetLimitsY(spec.Min, spec.Max);
        ApplyXLabels(plot, xs, spec.XLabels, spec.ShowXAxis);
    }

    private static void RenderMultiLine(Plot plot, ChartSpecs.MultiLineChartSpec spec)
    {
        double[] xs = [.. Enumerable.Range(0, spec.XLabels.Count).Select(i => (double)i)];

        foreach (ChartSpecs.LineSeries series in spec.Series)
        {
            Scatter scatter = plot.Add.Scatter(xs, [.. series.Data]);
            scatter.Color = Color.FromHex(series.ColorHex);
            scatter.LineWidth = 3;
            scatter.MarkerSize = 0;
            scatter.LegendText = series.Label;
        }

        plot.Axes.SetLimitsY(spec.Min, spec.Max);
        plot.Axes.Bottom.IsVisible = false;
        plot.ShowLegend();
    }

    private static byte[] RenderGauge(Plot plot, ChartSpecs.GaugeSpec spec)
    {
        // Semicírculo estilo velocímetro: un solo anillo con 2 segmentos (valor + resto hasta 100),
        // arrancando a la izquierda (180°) y barriendo 180° hasta la derecha, pasando por arriba.
        double value = Math.Clamp(spec.Value, 0, 100);
        RadialGaugePlot gauge = plot.Add.RadialGaugePlot([value, 100 - value]);
        gauge.GaugeMode = RadialGaugeMode.SingleGauge;
        gauge.Colors = [GaugeColor(value), ChartTheme.GridColor];
        gauge.ShowLevels = false;
        gauge.StartingAngle = 180;
        gauge.MaximumAngle = 180;
        gauge.StartCap = SKStrokeCap.Round;
        gauge.EndCap = SKStrokeCap.Round;

        const double contentBottom = -.3;
        Text label = plot.Add.Text($"{value:0}%", 0, -.1);
        label.LabelFontSize = 32;
        label.LabelBold = true;
        label.LabelFontColor = ChartTheme.TextColor;
        label.Alignment = Alignment.MiddleCenter;

        plot.Axes.Frameless();
        plot.HideGrid();

        byte[] fullImage = plot.GetImageBytes(spec.Width, spec.Height, ImageFormat.Png);

        // El gauge reserva su eje Y como si fuera un círculo completo (ignora los límites manuales
        // y se recalcula en cada render), dejando un vacío grande abajo ya que solo dibujamos el
        // semicírculo superior. Recortamos en espacio de píxeles hasta donde termina el contenido
        // real (arco + label), usando los límites reales que terminó usando el autoscale.
        AxisLimits limits = plot.Axes.GetLimits();
        double keepFraction = Math.Clamp((limits.Top - contentBottom) / (limits.Top - limits.Bottom), .3, 1);

        using SKBitmap bitmap = SKBitmap.Decode(fullImage);
        int cropHeight = Math.Max(1, (int)(bitmap.Height * keepFraction));
        using SKBitmap cropped = new(bitmap.Width, cropHeight);
        using (SKCanvas canvas = new(cropped))
            canvas.DrawBitmap(bitmap, new SKRect(0, 0, bitmap.Width, cropHeight), new SKRect(0, 0, bitmap.Width, cropHeight));

        using SKData data = cropped.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static Color GaugeColor(double value) => value switch
    {
        >= 75 => Color.FromHex("#ED4245"),
        >= 40 => Color.FromHex("#FEE75C"),
        _ => Color.FromHex("#57F287")
    };

    private static void ApplyXLabels(Plot plot, double[] xs, IReadOnlyList<string> labels, bool showAxis)
    {
        if (!showAxis)
        {
            plot.Axes.Bottom.IsVisible = false;
            return;
        }

        Tick[] ticks = [.. xs.Select((x, i) => new Tick(x, labels[i]))];
        plot.Axes.Bottom.TickGenerator = new NumericManual(ticks);
        plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
        // Con rotación, ScottPlot subestima el alto necesario para las etiquetas y las corta.
        plot.Axes.Bottom.MinimumSize = 60;
    }
}
