using Google.Cloud.Firestore;
using System;

namespace AnilistESP
{
    [FirestoreData]
    public class HighlightFirebase
    {
        [FirestoreProperty]
        public long UserId { get; set; }

        [FirestoreProperty]
        public string Highlight { get; set; }
    }
}
