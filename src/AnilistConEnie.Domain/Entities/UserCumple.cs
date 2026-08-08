namespace AnilistConEnie.Domain.Entities;

public record UserCumple
{
    public long Id { get; init; }

    /// <summary>Fecha del próximo festejo (este año si todavía no pasó; el que viene si ya pasó).</summary>
    public DateTime Proximo { get; init; }
}
