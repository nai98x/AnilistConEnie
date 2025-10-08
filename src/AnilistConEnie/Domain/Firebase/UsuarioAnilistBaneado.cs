using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class UsuarioAnilistBaneado
{
    [FirestoreProperty]
    public int AnilistUserId { get; set; }
}