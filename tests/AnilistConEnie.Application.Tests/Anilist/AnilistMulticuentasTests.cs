using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Application.Tests.Anilist;

public class AnilistMulticuentasTests
{
    private static UsuarioAnilist Vinculado(long userId, int anilistId) =>
        new() { UserId = userId, AnilistURL = $"https://anilist.co/user/{anilistId}" };

    [Fact]
    public void Detectar_NoRepeatedProfiles_ReturnsEmpty()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
            [Vinculado(111, 1), Vinculado(222, 2)]);

        Assert.Empty(grupos);
    }

    [Fact]
    public void Detectar_ProfileWithTwoDiscords_ReturnsGroup()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
            [Vinculado(111, 999), Vinculado(222, 999)]);

        GrupoMulticuenta grupo = Assert.Single(grupos);
        Assert.Equal(999, grupo.AnilistId);
        Assert.Equal(new long[] { 111, 222 }, grupo.DiscordIds);
    }

    [Fact]
    public void Detectar_SameDiscordRepeated_IsNotMulticuenta()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
            [Vinculado(111, 999), Vinculado(111, 999)]);

        Assert.Empty(grupos);
    }

    [Fact]
    public void Detectar_InvalidUrls_AreIgnored()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
            [new() { UserId = 111, AnilistURL = "https://anilist.co/user/Nombre" },
             new() { UserId = 222, AnilistURL = "https://anilist.co/user/Nombre" }]);

        Assert.Empty(grupos);
    }

    [Fact]
    public void Detectar_SeveralGroups_SortsByCountDescending()
    {
        IReadOnlyList<GrupoMulticuenta> grupos = AnilistMulticuentas.Detectar(
        [
            Vinculado(111, 1), Vinculado(222, 1),
            Vinculado(333, 2), Vinculado(444, 2), Vinculado(555, 2)
        ]);

        Assert.Equal(new[] { 2, 1 }, grupos.Select(g => g.AnilistId));
        Assert.Equal(3, grupos[0].DiscordIds.Count);
    }

    [Fact]
    public void AgruparPorAnilistId_SeveralProfiles_GroupsDiscordIdsByProfileInOrder()
    {
        Dictionary<int, List<long>> grupos = AnilistMulticuentas.AgruparPorAnilistId(
            [Vinculado(111, 1), Vinculado(222, 2), Vinculado(333, 1)]);

        Assert.Equal(2, grupos.Count);
        Assert.Equal(new long[] { 111, 333 }, grupos[1]);
        Assert.Equal(new long[] { 222 }, grupos[2]);
    }

    [Fact]
    public void AgruparPorAnilistId_SameDiscordTwice_KeepsItOnce()
    {
        Dictionary<int, List<long>> grupos = AnilistMulticuentas.AgruparPorAnilistId(
            [Vinculado(111, 1), Vinculado(111, 1)]);

        Assert.Equal(new long[] { 111 }, grupos[1]);
    }

    [Fact]
    public void AgruparPorAnilistId_InvalidOrMissingUrl_IsSkipped()
    {
        Dictionary<int, List<long>> grupos = AnilistMulticuentas.AgruparPorAnilistId(
        [
            new UsuarioAnilist { UserId = 111, AnilistURL = "https://anilist.co/user/Nombre" },
            new UsuarioAnilist { UserId = 222, AnilistURL = string.Empty },
            Vinculado(333, 5)
        ]);

        Assert.Equal([5], grupos.Keys);
    }
}
