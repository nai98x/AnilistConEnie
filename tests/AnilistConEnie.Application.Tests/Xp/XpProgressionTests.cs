using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpProgressionTests
{
    [Fact]
    public void Describe_ComputesNextRankAndMissingXp()
    {
        // 5000 de total: próximo rango Casual (10000), faltan 5000.
        XpProgressionInfo info = XpProgression.Describe(total: 5000, promedioDiario: 0);

        Assert.Equal(RangoEnum.Casual, info.NextRango);
        Assert.Equal(10000, info.NextRangeXp);
        Assert.Equal(5000, info.NextXp);
        // ceil((5000 - 0) / 15) = 334
        Assert.Equal(334, info.MensajesNecesarios);
    }

    [Fact]
    public void Describe_DiscountsDailyAverageFromNeededMessages()
    {
        XpProgressionInfo info = XpProgression.Describe(total: 5000, promedioDiario: 1000);

        // ceil((5000 - 1000) / 15) = 267
        Assert.Equal(267, info.MensajesNecesarios);
    }

    [Fact]
    public void Describe_NeverReturnsNegativeMessages()
    {
        XpProgressionInfo info = XpProgression.Describe(total: 9999, promedioDiario: 100000);

        Assert.Equal(0, info.MensajesNecesarios);
    }

    [Theory]
    [InlineData(RangoEnum.Tama, 499, true)]
    [InlineData(RangoEnum.Tama, 500, false)]
    [InlineData(RangoEnum.Casual, 999, true)]
    [InlineData(RangoEnum.Kouhai, 2499, true)]
    [InlineData(RangoEnum.Senpai, 2500, false)]
    [InlineData(RangoEnum.Hikikomori, 4999, true)]
    [InlineData(RangoEnum.Sensei, 5000, false)]
    [InlineData(RangoEnum.Ousama, 7499, true)]
    [InlineData(RangoEnum.Teiou, 9999, true)]
    [InlineData(RangoEnum.Teiou, 10000, false)]
    [InlineData(RangoEnum.Miembro, 0, false)]
    public void EstaProximoASubir_PerRankThreshold(RangoEnum nextRango, long nextXp, bool esperado)
    {
        Assert.Equal(esperado, XpProgression.EstaProximoASubir(nextRango, nextXp));
    }

    [Fact]
    public void EstimarTiempo_NoAverage_CannotEstimate()
    {
        string result = XpProgression.EstimarTiempo(nextXp: 5000, promedioXp: 0, "Casual", mensajes: 10);

        Assert.Contains("No se puede estimar", result);
        Assert.Contains("Casual", result);
    }

    [Fact]
    public void EstimarTiempo_RankAlreadyReached_Congratulates()
    {
        string result = XpProgression.EstimarTiempo(nextXp: 0, promedioXp: 100, "Teiou", mensajes: 0);

        Assert.Contains("Felicidades", result);
        Assert.Contains("Teiou", result);
    }

    [Fact]
    public void EstimarTiempo_LessThanAYearLeft_ReturnsDays()
    {
        // ceil(3000 / 100) = 30 días
        string result = XpProgression.EstimarTiempo(nextXp: 3000, promedioXp: 100, "Casual", mensajes: 200);

        Assert.Contains("30 días", result);
        Assert.DoesNotContain("año", result);
    }

    [Fact]
    public void EstimarTiempo_WholeYears_ReturnsYears()
    {
        // 73000 / 100 = 730 días = 2 años exactos
        string result = XpProgression.EstimarTiempo(nextXp: 73000, promedioXp: 100, "Teiou", mensajes: 5000);

        Assert.Contains("2 año(s)", result);
        Assert.DoesNotContain("y ", result);
    }

    [Fact]
    public void EstimarTiempo_YearsAndRemainder_ReturnsYearsAndDays()
    {
        // ceil(40000 / 100) = 400 días = 1 año y 35 días
        string result = XpProgression.EstimarTiempo(nextXp: 40000, promedioXp: 100, "Teiou", mensajes: 5000);

        Assert.Contains("1 año(s) y 35 días", result);
    }
}
