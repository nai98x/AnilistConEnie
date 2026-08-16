namespace AnilistConEnie.Domain.Entities;

public class ChallengeCompletado
{
    public long UserId { get; set; }

    public int Xp { get; set; }

    public DateTimeOffset Date { get; set; }

    public int Completados { get; set; }
}
