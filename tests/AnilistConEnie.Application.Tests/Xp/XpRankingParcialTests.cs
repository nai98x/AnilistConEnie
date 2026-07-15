using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpRankingParcialTests
{
    private static UserXp User(long id, long total) => new() { UserId = id, Total = total };

    [Fact]
    public void InicioRango_Diario_EsHoy()
    {
        DateOnly hoy = new(2026, 7, 15);

        Assert.Equal(hoy, XpRankingParcial.InicioRango(XpRangoParcial.Diario, hoy));
    }

    [Theory]
    [InlineData(2026, 7, 15, 2026, 7, 13)] // miércoles -> lunes de la misma semana
    [InlineData(2026, 7, 13, 2026, 7, 13)] // lunes -> el mismo día
    [InlineData(2026, 7, 19, 2026, 7, 13)] // domingo -> lunes anterior (DayOfWeek.Sunday == 0)
    [InlineData(2026, 7, 1, 2026, 6, 29)]  // semana que cruza de mes
    [InlineData(2026, 1, 2, 2025, 12, 29)] // semana que cruza de año
    public void InicioRango_Semanal_EsElLunesDeLaSemana(int y, int m, int d, int ey, int em, int ed)
    {
        Assert.Equal(new DateOnly(ey, em, ed), XpRankingParcial.InicioRango(XpRangoParcial.Semanal, new DateOnly(y, m, d)));
    }

    [Fact]
    public void InicioRango_Mensual_EsElPrimeroDelMes()
    {
        Assert.Equal(new DateOnly(2026, 7, 1), XpRankingParcial.InicioRango(XpRangoParcial.Mensual, new DateOnly(2026, 7, 15)));
    }

    [Fact]
    public void InicioRango_Anual_EsElPrimeroDeEnero()
    {
        Assert.Equal(new DateOnly(2026, 1, 1), XpRankingParcial.InicioRango(XpRangoParcial.Anual, new DateOnly(2026, 7, 15)));
    }

    [Fact]
    public void Build_OrdenaPorDeltaDescendenteConRanks()
    {
        List<UserXp> totales = [User(1, 500), User(2, 1000), User(3, 300)];
        Dictionary<long, long> baselines = new() { [1] = 100, [2] = 900, [3] = 0 };

        IReadOnlyList<XpRankEntry> ranking = XpRankingParcial.Build(totales, baselines);

        Assert.Equal(3, ranking.Count);
        Assert.Equal((1, 400L, 1ul), (ranking[0].Rank, ranking[0].Score, ranking[0].UserId));
        Assert.Equal((2, 300L, 3ul), (ranking[1].Rank, ranking[1].Score, ranking[1].UserId));
        Assert.Equal((3, 100L, 2ul), (ranking[2].Rank, ranking[2].Score, ranking[2].UserId));
    }

    [Fact]
    public void Build_UsuarioSinBaseline_CuentaTodoSuTotal()
    {
        IReadOnlyList<XpRankEntry> ranking = XpRankingParcial.Build([User(1, 250)], new Dictionary<long, long>());

        Assert.Single(ranking);
        Assert.Equal(250, ranking[0].Score);
    }

    [Fact]
    public void Build_DeltaCero_QuedaAlFinalDelRanking()
    {
        List<UserXp> totales = [User(1, 100), User(3, 200)];
        Dictionary<long, long> baselines = new() { [1] = 100, [3] = 50 };

        IReadOnlyList<XpRankEntry> ranking = XpRankingParcial.Build(totales, baselines);

        Assert.Equal(2, ranking.Count);
        Assert.Equal((1, 150L, 3ul), (ranking[0].Rank, ranking[0].Score, ranking[0].UserId));
        Assert.Equal((2, 0L, 1ul), (ranking[1].Rank, ranking[1].Score, ranking[1].UserId));
    }

    [Fact]
    public void Build_DeltaNegativo_QuedaExcluido()
    {
        List<UserXp> totales = [User(2, 100), User(3, 200)];
        Dictionary<long, long> baselines = new() { [2] = 150, [3] = 50 };

        IReadOnlyList<XpRankEntry> ranking = XpRankingParcial.Build(totales, baselines);

        Assert.Single(ranking);
        Assert.Equal(3ul, ranking[0].UserId);
    }

    [Fact]
    public void Build_ListaVacia_RankingVacio()
    {
        Assert.Empty(XpRankingParcial.Build([], new Dictionary<long, long>()));
    }

    [Fact]
    public void Build_ResultadoCompatibleConResolvePosition()
    {
        List<UserXp> totales = [User(1, 500), User(2, 300)];
        Dictionary<long, long> baselines = new() { [1] = 100, [2] = 100 };

        IReadOnlyList<XpRankEntry> ranking = XpRankingParcial.Build(totales, baselines);
        RankPosition pos = XpRanking.ResolvePosition(ranking, userId: 2);

        Assert.Equal(RankPositionKind.Behind, pos.Kind);
        Assert.Equal(2, pos.PositionNumber);
        Assert.Equal(200, pos.Diff);
        Assert.Equal(1ul, pos.RivalUserId);
    }
}
