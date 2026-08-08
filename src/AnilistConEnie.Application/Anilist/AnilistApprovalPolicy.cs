using AnilistConEnie.Domain.Entities;

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

        if (AnilistMulticuentas.AgruparPorAnilistId(vinculados).TryGetValue(anilistViewerId, out List<long>? mismoPerfil))
        {
            long? otroMiembro = mismoPerfil
                .Where(id => (ulong)id != discordUserId)
                .Select(id => (long?)id)
                .FirstOrDefault();
            if (otroMiembro is not null)
                motivos.Add(new(MotivoAprobacion.PosibleMulticuenta, otroMiembro));
        }

        DateTimeOffset umbral = ahora.AddMonths(-cuentaNuevaMeses);
        if (discordCreatedAt > umbral || anilistCreatedAt > umbral)
            motivos.Add(new(MotivoAprobacion.CuentaReciente));

        return motivos;
    }
}
