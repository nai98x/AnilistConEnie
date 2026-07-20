using AnilistConEnie.Application.Fun;

namespace AnilistConEnie.Application.Tests.Fun;

public class TrueLoveCalculatorTests
{
    [Fact]
    public void Afinidad_MismaPareja_EsDeterministica()
    {
        Assert.Equal(TrueLoveCalculator.Afinidad(10, 20), TrueLoveCalculator.Afinidad(10, 20));
    }

    [Fact]
    public void Afinidad_DentroDe0A100()
    {
        for (ulong c = 1; c < 200; c++)
            Assert.InRange(TrueLoveCalculator.Afinidad(500, c), 0, 100);
    }

    [Fact]
    public void Calcular_SinCandidatos_MatchNulo()
    {
        TrueLoveResult r = TrueLoveCalculator.Calcular(1, []);

        Assert.Null(r.MatchId);
        Assert.Equal(0, r.MaxPorcentaje);
        Assert.Empty(r.Pretendientes);
        Assert.Empty(r.Odiados);
    }

    [Fact]
    public void Calcular_MatchEsElDeMayorAfinidad()
    {
        ulong target = 777;
        List<ulong> candidatos = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        TrueLoveResult r = TrueLoveCalculator.Calcular(target, candidatos);

        ulong esperado = candidatos.MaxBy(c => TrueLoveCalculator.Afinidad(target, c));
        Assert.Equal(esperado, r.MatchId);
        Assert.Equal(TrueLoveCalculator.Afinidad(target, esperado), r.MaxPorcentaje);
    }

    [Fact]
    public void Calcular_TopSeLimitaA5YOrdenaCorrecto()
    {
        List<ulong> candidatos = Enumerable.Range(1, 20).Select(i => (ulong)i).ToList();

        TrueLoveResult r = TrueLoveCalculator.Calcular(333, candidatos);

        Assert.Equal(5, r.Pretendientes.Count);
        Assert.Equal(5, r.Odiados.Count);
        Assert.Equal(r.Pretendientes.Select(x => x.Porcentaje).OrderByDescending(x => x), r.Pretendientes.Select(x => x.Porcentaje));
        Assert.Equal(r.Odiados.Select(x => x.Porcentaje).OrderBy(x => x), r.Odiados.Select(x => x.Porcentaje));
    }

    [Fact]
    public void Calcular_EsDeterministico()
    {
        List<ulong> candidatos = [11, 22, 33, 44];

        TrueLoveResult a = TrueLoveCalculator.Calcular(9, candidatos);
        TrueLoveResult b = TrueLoveCalculator.Calcular(9, candidatos);

        Assert.Equal(a.MatchId, b.MatchId);
        Assert.Equal(a.MaxPorcentaje, b.MaxPorcentaje);
        Assert.Equal(a.Pretendientes, b.Pretendientes);
        Assert.Equal(a.Odiados, b.Odiados);
    }
}
