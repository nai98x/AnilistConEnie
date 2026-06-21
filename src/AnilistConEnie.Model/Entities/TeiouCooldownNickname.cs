using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class TeiouCooldownNickname
{
    [FirestoreProperty]
    public long UserId { get; set; }

    [FirestoreProperty]
    public DateTime Cooldown { get; set; }
}
