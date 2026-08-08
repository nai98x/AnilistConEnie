using AnilistConEnie.Application.Charts;
using AnilistConEnie.Domain.Entities.Charts;

namespace AnilistConEnie.Application.Tests.Charts;

public class FunChartsTests
{
    [Fact]
    public void BoludoGauge_UsaElValor()
    {
        GaugeSpec spec = FunCharts.BoludoGauge(73);
        Assert.Equal(73, spec.Value);
    }

    [Fact]
    public void BoludoLine_MapeaValoresYEtiquetas()
    {
        LineChartSpec spec = FunCharts.BoludoLine("Fulano", [1, 2, 3], [10, 20, 30]);

        Assert.Equal([10d, 20d, 30d], spec.Values);
        Assert.Equal(["1", "2", "3"], spec.XLabels);
        Assert.Equal(0, spec.Min);
        Assert.Equal(100, spec.Max);
        Assert.False(spec.Fill);
        Assert.Equal("Boludómetro de Fulano", spec.Label);
    }
}
