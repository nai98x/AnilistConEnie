using Google.Cloud.Firestore;

namespace AnilistESP
{
    [FirestoreData]
    public class UsuarioAnilistFirebase
    {
        [FirestoreProperty]
        public string AnilistURL { get; set; }

        [FirestoreProperty]
        public long MessageId { get; set; }

        [FirestoreProperty]
        public long UserId { get; set; }
    }
}
