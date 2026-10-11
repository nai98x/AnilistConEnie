using AnilistConEnie.Application.Actividad;

namespace AnilistConEnie.Application.Tests.Actividad;

public class RolActivoServidorTests
{
    [Fact]
    public void Ventana_TerminaAyerYAbarcaLosDias()
    {
        (DateOnly desde, DateOnly hasta) = RolActivoServidor.Ventana(new DateOnly(2026, 10, 10), 30);

        Assert.Equal(new DateOnly(2026, 9, 10), desde);
        Assert.Equal(new DateOnly(2026, 10, 9), hasta);
    }

    private static readonly DateTimeOffset Ahora = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EntradaAntigua = Ahora.AddMonths(-6);

    [Fact]
    public void Corresponde_SuperaMensajesYDias() =>
        Assert.True(RolActivoServidor.Corresponde(31, 7, 30, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_ExactamenteElUmbralDeMensajes_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(30, 10, 30, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_PocosDiasDistintos_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(500, 6, 30, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_SinMensajes_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(0, 0, 30, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_EntroHaceMenosDeUnMes_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(500, 20, 30, 7, Ahora.AddMonths(-1).AddDays(1), Ahora, 1));

    [Fact]
    public void Corresponde_EntroHaceExactamenteUnMes_Corresponde() =>
        Assert.True(RolActivoServidor.Corresponde(500, 20, 30, 7, Ahora.AddMonths(-1), Ahora, 1));
}
