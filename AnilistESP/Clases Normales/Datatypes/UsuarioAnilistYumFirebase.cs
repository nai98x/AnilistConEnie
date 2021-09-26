using Google.Cloud.Firestore;
using System;

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
