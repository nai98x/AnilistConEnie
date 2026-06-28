using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Anilist;

public enum MotivoAprobacion
{
    PosibleMulticuenta,
    CuentaReciente
}

public sealed record MotivoAprobacionDetalle(MotivoAprobacion Motivo, long? OtroMiembroId = null);

public static class AnilistApprovalPolicy
{
    public static IReadOnlyList<MotivoAprobacionDetalle> Evaluar(
        IReadOnlyList<UsuarioAnilist> vinculados,
        ulong discordUserId,
        int anilistViewerId,
        DateTimeOffset discordCreatedAt,
        DateTimeOffset anilistCreatedAt,
        int cuentaNuevaMeses,
        DateTimeOffset ahora)
    {
        List<MotivoAprobacionDetalle> motivos = [];

        UsuarioAnilist? multicuenta = vinculados.FirstOrDefault(x =>
            (ulong)x.UserId != discordUserId
            && AnilistProfileUrl.TryGetUserId(x.AnilistURL, out int id) && id == anilistViewerId);
        if (multicuenta is not null)
            motivos.Add(new(MotivoAprobacion.PosibleMulticuenta, multicuenta.UserId));

        DateTimeOffset umbral = ahora.AddMonths(-cuentaNuevaMeses);
        if (discordCreatedAt > umbral || anilistCreatedAt > umbral)
            motivos.Add(new(MotivoAprobacion.CuentaReciente));

        return motivos;
    }
}
