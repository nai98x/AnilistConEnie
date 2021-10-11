using Google.Cloud.Firestore;
using System;

namespace AnilistESP
{
    [FirestoreData]
    public class IntercambiosFirebase
    {
        [FirestoreProperty]
        public long UserId { get; set; }

        [FirestoreProperty]
        public string Pref1 { get; set; }

        [FirestoreProperty]
        public string Pref2 { get; set; }

        [FirestoreProperty]
        public string Ban { get; set; }

        [FirestoreProperty]
        public int Orden { get; set; }
    }
}
