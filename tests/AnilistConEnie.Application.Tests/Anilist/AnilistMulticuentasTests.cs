using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Application.Tests.Anilist;

public class AnilistMulticuentasTests
{
    private static UsuarioAnilist Vinculado(long userId, int anilistId) =>
        new() { UserId = userId, AnilistURL = $"https://anilist.co/user/{anilistId}" };

    [Fact]
    public void Detectar_SinPerfilesRepetidos_DevuelveVacio()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
            [Vinculado(111, 1), Vinculado(222, 2)]);

        Assert.Empty(grupos);
    }

    [Fact]
    public void Detectar_PerfilConDosDiscord_DevuelveGrupo()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
            [Vinculado(111, 999), Vinculado(222, 999)]);

        GrupoMulticuenta grupo = Assert.Single(grupos);
        Assert.Equal(999, grupo.AnilistId);
        Assert.Equal(new long[] { 111, 222 }, grupo.DiscordIds);
    }

    [Fact]
    public void Detectar_MismoDiscordRepetido_NoCuentaComoMulticuenta()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
            [Vinculado(111, 999), Vinculado(111, 999)]);

        Assert.Empty(grupos);
    }

    [Fact]
    public void Detectar_IgnoraUrlsInvalidas()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
            [new() { UserId = 111, AnilistURL = "https://anilist.co/user/Nombre" },
             new() { UserId = 222, AnilistURL = "https://anilist.co/user/Nombre" }]);

        Assert.Empty(grupos);
    }

    [Fact]
    public void Detectar_OrdenaPorCantidadDescendente()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
        [
            Vinculado(111, 1), Vinculado(222, 1),
            Vinculado(333, 2), Vinculado(444, 2), Vinculado(555, 2)
        ]);

        Assert.Equal(new[] { 2, 1 }, grupos.Select(g => g.AnilistId));
        Assert.Equal(3, grupos[0].DiscordIds.Count);
    }
}
