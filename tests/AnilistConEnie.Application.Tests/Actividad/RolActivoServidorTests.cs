using AnilistConEnie.Application.Actividad;

namespace AnilistConEnie.Application.Tests.Actividad;

public class RolActivoServidorTests
{
    [Fact]
    public void Ventana_TerminaHoyYAbarcaLosDias()
    {
        (DateOnly desde, DateOnly hasta) = RolActivoServidor.Ventana(new DateOnly(2026, 10, 10), 30);

        Assert.Equal(new DateOnly(2026, 9, 11), desde);
        Assert.Equal(new DateOnly(2026, 10, 10), hasta);
    }

    private static readonly DateTimeOffset Ahora = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EntradaAntigua = Ahora.AddMonths(-6);

    [Fact]
    public void Corresponde_SuperaMensajesYDias() =>
        Assert.True(RolActivoServidor.Corresponde(31, 0, 7, 30, 10, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_ExactamenteElUmbralDeMensajes_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(30, 0, 10, 30, 10, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_PocosDiasDistintos_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(500, 0, 6, 30, 10, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_SinMensajes_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(0, 0, 0, 30, 10, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_EntroHaceMenosDeUnMes_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(500, 0, 20, 30, 10, 7, Ahora.AddMonths(-1).AddDays(1), Ahora, 1));

    [Fact]
    public void Corresponde_EntroHaceExactamenteUnMes_Corresponde() =>
        Assert.True(RolActivoServidor.Corresponde(500, 0, 20, 30, 10, 7, Ahora.AddMonths(-1), Ahora, 1));

    [Fact]
    public void Corresponde_TodoEnGeneral_Corresponde() =>
        Assert.True(RolActivoServidor.Corresponde(31, 0, 7, 30, 10, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_TodoEnUnCanalQueNoEsGeneral_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(31, 31, 7, 30, 10, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_PocosFueraDelCanalPrincipal_NoCorresponde() =>
        Assert.False(RolActivoServidor.Corresponde(31, 22, 7, 30, 10, 7, EntradaAntigua, Ahora, 1));

    [Fact]
    public void Corresponde_ExactamenteElMinimoFueraDelCanalPrincipal_Corresponde() =>
        Assert.True(RolActivoServidor.Corresponde(31, 21, 7, 30, 10, 7, EntradaAntigua, Ahora, 1));
}
