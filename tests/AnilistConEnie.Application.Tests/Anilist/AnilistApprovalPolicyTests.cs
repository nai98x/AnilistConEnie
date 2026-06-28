using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Anilist;

public class AnilistApprovalPolicyTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Vieja = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static UsuarioAnilist Vinculado(long userId, int anilistId) =>
        new() { UserId = userId, AnilistURL = $"https://anilist.co/user/{anilistId}" };

    [Fact]
    public void Evaluar_NoMulticuentaNorRecentAccount_ReturnsNoReasons()
    {
        IReadOnlyList<MotivoAprobacionDetalle> motivos = AnilistApprovalPolicy.Evaluar(
            [Vinculado(111, 999)], discordUserId: 222, anilistViewerId: 888,
            discordCreatedAt: Vieja, anilistCreatedAt: Vieja, cuentaNuevaMeses: 1, ahora: Ahora);

        Assert.Empty(motivos);
    }

    [Fact]
    public void Evaluar_AnilistLinkedToAnotherMember_FlagsMulticuenta()
    {
        IReadOnlyList<MotivoAprobacionDetalle> motivos = AnilistApprovalPolicy.Evaluar(
            [Vinculado(111, 999)], discordUserId: 222, anilistViewerId: 999,
            discordCreatedAt: Vieja, anilistCreatedAt: Vieja, cuentaNuevaMeses: 1, ahora: Ahora);

        MotivoAprobacionDetalle motivo = Assert.Single(motivos);
        Assert.Equal(MotivoAprobacion.PosibleMulticuenta, motivo.Motivo);
        Assert.Equal(111, motivo.OtroMiembroId);
    }

    [Fact]
    public void Evaluar_AnilistBelongsToSameUser_DoesNotFlagMulticuenta()
    {
        IReadOnlyList<MotivoAprobacionDetalle> motivos = AnilistApprovalPolicy.Evaluar(
            [Vinculado(222, 999)], discordUserId: 222, anilistViewerId: 999,
            discordCreatedAt: Vieja, anilistCreatedAt: Vieja, cuentaNuevaMeses: 1, ahora: Ahora);

        Assert.Empty(motivos);
    }

    [Fact]
    public void Evaluar_RecentDiscordAccount_FlagsCuentaReciente()
    {
        IReadOnlyList<MotivoAprobacionDetalle> motivos = AnilistApprovalPolicy.Evaluar(
            [], discordUserId: 222, anilistViewerId: 888,
            discordCreatedAt: Ahora.AddDays(-3), anilistCreatedAt: Vieja, cuentaNuevaMeses: 1, ahora: Ahora);

        Assert.Equal(MotivoAprobacion.CuentaReciente, Assert.Single(motivos).Motivo);
    }

    [Fact]
    public void Evaluar_RecentAnilistAccount_FlagsCuentaReciente()
    {
        IReadOnlyList<MotivoAprobacionDetalle> motivos = AnilistApprovalPolicy.Evaluar(
            [], discordUserId: 222, anilistViewerId: 888,
            discordCreatedAt: Vieja, anilistCreatedAt: Ahora.AddDays(-3), cuentaNuevaMeses: 1, ahora: Ahora);

        Assert.Equal(MotivoAprobacion.CuentaReciente, Assert.Single(motivos).Motivo);
    }

    [Fact]
    public void Evaluar_MultipleReasons_ReturnsMulticuentaFirst()
    {
        IReadOnlyList<MotivoAprobacionDetalle> motivos = AnilistApprovalPolicy.Evaluar(
            [Vinculado(111, 999)], discordUserId: 222, anilistViewerId: 999,
            discordCreatedAt: Ahora.AddDays(-3), anilistCreatedAt: Vieja, cuentaNuevaMeses: 1, ahora: Ahora);

        Assert.Equal(
            new[] { MotivoAprobacion.PosibleMulticuenta, MotivoAprobacion.CuentaReciente },
            motivos.Select(m => m.Motivo));
    }
}
