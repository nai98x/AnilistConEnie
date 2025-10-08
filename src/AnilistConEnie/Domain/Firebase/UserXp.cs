using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class UserXp
{
    [FirestoreProperty]
    public long UserId { get; set; }

    [FirestoreProperty]
    public long Total { get; set; }

    [FirestoreProperty]
    public long Booster { get; set; }

    [FirestoreProperty]
    public long Challenges { get; set; }

    [FirestoreProperty]
    public long Eventos { get; set; }

    [FirestoreProperty]
    public long Intercambios { get; set; }

    [FirestoreProperty]
    public long Otros { get; set; }
}
