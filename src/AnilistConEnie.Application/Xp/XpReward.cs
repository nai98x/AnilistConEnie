namespace AnilistConEnie.Application.Xp;

/// <summary>
/// Resultado de acreditar XP a un usuario en un tick: cuánto se otorgó, desglosado en base y booster.
/// </summary>
public readonly record struct XpAccrual(
    int BaseXp,
    int BoosterXp,
    int TotalGranted);

/// <summary>
/// Regla de negocio de cuánta XP gana un usuario por tick de actividad: una base aleatoria y, si está
/// boosteando el servidor, un extra aleatorio adicional.
/// </summary>
public static class XpReward
{
    public static XpAccrual Accrue(bool isBooster, Random rng, int minPorMensaje, int maxPorMensaje, int minBooster, int maxBooster)
    {
        int baseXp = rng.Next(minPorMensaje, maxPorMensaje + 1);
        int boosterXp = isBooster ? rng.Next(minBooster, maxBooster + 1) : 0;

        return new XpAccrual(baseXp, boosterXp, baseXp + boosterXp);
    }
}
