namespace AnilistConEnie.Model.Entities;

public class ChallengeCompletado
{
    public long UserId { get; set; }

    public int Xp { get; set; }

    public DateTimeOffset Date { get; set; }
}
