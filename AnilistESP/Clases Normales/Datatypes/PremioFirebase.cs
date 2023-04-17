using Google.Cloud.Firestore;
using System;

namespace AnilistESP
{
    [FirestoreData]
    public class PremioFirebase
    {
        [FirestoreProperty]
        public string Link { get; set; }

        [FirestoreProperty]
        public string Nombre { get; set; }

        [FirestoreProperty]
        public int Year { get; set; }

        [FirestoreProperty]
        public int Order { get; set; }
    }
}
