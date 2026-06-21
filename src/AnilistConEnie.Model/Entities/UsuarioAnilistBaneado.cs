using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class UsuarioAnilistBaneado
{
    [FirestoreProperty]
    public int AnilistUserId { get; set; }
}
