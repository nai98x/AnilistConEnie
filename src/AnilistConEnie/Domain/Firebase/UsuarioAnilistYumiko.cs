using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class UsuarioAnilistYumiko
{
    [FirestoreProperty]
    public int AnilistId { get; set; }

    [FirestoreProperty]
    public long UserId { get; set; }
}