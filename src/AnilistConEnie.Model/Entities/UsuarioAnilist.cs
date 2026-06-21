using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class UsuarioAnilist
{
    [FirestoreProperty]
    public string AnilistURL { get; set; } = string.Empty;

    [FirestoreProperty]
    public long MessageId { get; set; }

    [FirestoreProperty]
    public long UserId { get; set; }
}
