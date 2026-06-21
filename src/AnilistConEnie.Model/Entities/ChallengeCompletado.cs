using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class ChallengeCompletado
{
    [FirestoreProperty]
    public long UserId { get; set; }

    [FirestoreProperty]
    public int Xp { get; set; }

    [FirestoreProperty]
    public DateTimeOffset Date { get; set; }
}
