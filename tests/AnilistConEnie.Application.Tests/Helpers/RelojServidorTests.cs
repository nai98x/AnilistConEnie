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
    public void EnHoraLocal_ConvierteUtcAArgentina()
    {
        DateTime utc = new(2021, 7, 8, 2, 0, 0, DateTimeKind.Utc);

        DateTimeOffset local = RelojServidor.EnHoraLocal(utc);

        Assert.Equal(TimeSpan.FromHours(-3), local.Offset);
        Assert.Equal(new DateTime(2021, 7, 7, 23, 0, 0), local.DateTime);
    }

    [Fact]
    public void EnHoraLocal_AsumeUtcAunqueElKindSeaUnspecified()
    {
        DateTime unspecified = new(2023, 2, 23, 12, 44, 0, DateTimeKind.Unspecified);

        DateTimeOffset local = RelojServidor.EnHoraLocal(unspecified);

        Assert.Equal(new DateTime(2023, 2, 23, 9, 44, 0), local.DateTime);
    }

    [Fact]
    public void Hoy_EsLaFechaDeAhoraSinHora()
    {
        DateTime hoy = RelojServidor.Hoy;

        Assert.Equal(TimeSpan.Zero, hoy.TimeOfDay);
        Assert.Equal(RelojServidor.Ahora.Date, hoy);
    }
}
