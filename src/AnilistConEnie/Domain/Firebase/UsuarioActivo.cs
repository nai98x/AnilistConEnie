using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class UsuarioActivo
{
    [FirestoreProperty]
    public long UserId { get; set; }

    [FirestoreProperty]
    public DateTime LastActivity { get; set; }
}