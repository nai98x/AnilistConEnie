using Google.Cloud.Firestore;
using System;

namespace AnilistESP
{
    [FirestoreData]
    public class IntercambiosRecomendacionFirebase
    {
        [FirestoreProperty]
        public long UserId { get; set; }

        [FirestoreProperty]
        public long UserIdRecomendadoPor { get; set; }

        [FirestoreProperty]
        public string AnimeRecomendadoName { get; set; }

        [FirestoreProperty]
        public string AnimeRecomendadoURL { get; set; }

        [FirestoreProperty]
        public int VecesReclamada { get; set; }
    }
}
