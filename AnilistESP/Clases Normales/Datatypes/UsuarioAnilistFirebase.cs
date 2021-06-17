using Google.Cloud.Firestore;
using System;

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
