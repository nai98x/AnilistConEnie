namespace AnilistConEnie.Domain.Entities;

public class XpDiarioExtremos
{
    public long UserId { get; set; }

    public DateTime PrimeraFecha { get; set; }

    public long PrimerXp { get; set; }

    public DateTime UltimaFecha { get; set; }

    public long UltimoXp { get; set; }
}
