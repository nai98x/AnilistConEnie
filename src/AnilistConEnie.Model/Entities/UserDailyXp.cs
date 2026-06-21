using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class UserDailyXp
{
    [FirestoreProperty]
    public long UserId { get; set; }

    [FirestoreProperty]
    public long Xp { get; set; }

    [FirestoreProperty]
    public DateTime Date { get; set; }
}
