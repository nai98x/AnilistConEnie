using Google.Cloud.Firestore;

namespace AnilistESP
{
    [FirestoreData]
    public class UsuarioAnilistYumFirebase
    {
        [FirestoreProperty]
        public int AnilistId { get; set; }

        [FirestoreProperty]
        public long UserId { get; set; }
    }
}
