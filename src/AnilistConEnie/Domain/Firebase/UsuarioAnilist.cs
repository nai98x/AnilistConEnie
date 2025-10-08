using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

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