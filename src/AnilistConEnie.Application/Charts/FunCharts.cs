using AnilistConEnie.Model.Entities.Charts;

namespace AnilistConEnie.Application.Charts;

/// <summary>Arma los <see cref="ChartSpec"/> del comando "boludómetro" (Fun).</summary>
public static class FunCharts
{
    public static GaugeSpec BoludoGauge(int value) => new(value);

    public static LineChartSpec BoludoLine(string displayName, IEnumerable<int> dias, IEnumerable<int> valores) => new(
        Values: [.. valores.Select(v => (double)v)],
        XLabels: [.. dias.Select(d => d.ToString())],
        Min: 0,
        Max: 100,
        Fill: false,
        Label: $"Boludómetro de {displayName}");
}
