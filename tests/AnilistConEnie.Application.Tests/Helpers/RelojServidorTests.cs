using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class RelojServidorTests
{
    [Fact]
    public void Ahora_EsUtcMenosTresHoras()
    {
        TimeSpan diferencia = DateTime.UtcNow - RelojServidor.Ahora;

        // ART es UTC-3 fijo (sin DST); margen chico por el tiempo entre ambas lecturas.
        Assert.InRange(diferencia, TimeSpan.FromHours(3) - TimeSpan.FromSeconds(5), TimeSpan.FromHours(3) + TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Hoy_EsLaFechaDeAhoraSinHora()
    {
        DateTime hoy = RelojServidor.Hoy;

        Assert.Equal(TimeSpan.Zero, hoy.TimeOfDay);
        Assert.Equal(RelojServidor.Ahora.Date, hoy);
    }
}
