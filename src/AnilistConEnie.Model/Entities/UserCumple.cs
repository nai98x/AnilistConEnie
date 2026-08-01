namespace AnilistConEnie.Model.Entities;

public class UserCumple
{
    public long Id { get; set; }

    /// <summary>Fecha del próximo festejo (este año si todavía no pasó; el que viene si ya pasó).</summary>
    public DateTime Proximo { get; set; }
}
