namespace AnilistConEnie.Domain.Entities;

/// <summary>
/// Cambio a aplicar sobre la XP de un usuario, desglosado por categoría. Los valores son magnitudes;
/// si se suman o se restan lo decide la <see cref="Enum.XpOperation"/> que se pase al repositorio.
/// </summary>
public sealed record UserXpDelta
{
    public long Total { get; init; }
    public long Booster { get; init; }
    public long Challenges { get; init; }
    public long Eventos { get; init; }
    public long Intercambios { get; init; }
    public long Otros { get; init; }
}
