using AnilistConEnie.Application.Fun;

namespace AnilistConEnie.Application.Tests.Fun;

public class HoroscopoCalculatorTests
{
    [Fact]
    public void Puntajes_MismaEntrada_EsDeterministico()
    {
        Assert.Equal(
            HoroscopoCalculator.Puntajes(3, 200, 2026, 555),
            HoroscopoCalculator.Puntajes(3, 200, 2026, 555));
    }

    [Fact]
    public void Puntajes_DentroDe0A100()
    {
        for (int signo = 0; signo < 12; signo++)
        {
            (double amor, double salud, double dinero) = HoroscopoCalculator.Puntajes(signo, 100, 2026, 42);
            Assert.InRange(amor, 0, 100);
            Assert.InRange(salud, 0, 100);
            Assert.InRange(dinero, 0, 100);
        }
    }

    [Fact]
    public void Puntajes_DistintoUsuario_MismoSignoYDia_PuedeDiferir()
    {
        var a = HoroscopoCalculator.Puntajes(5, 150, 2026, 1);
        var b = HoroscopoCalculator.Puntajes(5, 150, 2026, 99999);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Puntajes_SonEnteros()
    {
        (double amor, double salud, double dinero) = HoroscopoCalculator.Puntajes(7, 80, 2026, 314);

        Assert.Equal(amor, Math.Round(amor, 0));
        Assert.Equal(salud, Math.Round(salud, 0));
        Assert.Equal(dinero, Math.Round(dinero, 0));
    }
}
