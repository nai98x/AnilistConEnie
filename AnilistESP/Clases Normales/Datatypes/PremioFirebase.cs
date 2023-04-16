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
    }
}
