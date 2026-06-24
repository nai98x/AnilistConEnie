using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Xp;

/// <summary>
/// Resultado de acreditar XP a un usuario en un tick: cuánto se otorgó (base + booster) y los nuevos
/// totales acumulados.
/// </summary>
public readonly record struct XpAccrual(
    int BaseXp,
    int BoosterXp,
    int TotalGranted,
    long NewBoosterTotal,
    long NewGrandTotal);

/// <summary>
/// Regla de negocio de cuánta XP gana un usuario por tick de actividad: una base aleatoria y, si está
/// boosteando el servidor, un extra aleatorio adicional.
/// </summary>
public static class XpReward
{
    public static XpAccrual Accrue(UserXp current, bool isBooster, Random rng, int minPorMensaje, int maxPorMensaje, int minBooster, int maxBooster)
    {
        int baseXp = rng.Next(minPorMensaje, maxPorMensaje + 1);
        int boosterXp = isBooster ? rng.Next(minBooster, maxBooster + 1) : 0;
        int totalGranted = baseXp + boosterXp;

        return new XpAccrual(
            baseXp,
            boosterXp,
            totalGranted,
            current.Booster + boosterXp,
            current.Total + totalGranted);
    }
}
