using Google.Cloud.Firestore;
using System;

namespace AnilistESP
{
    [FirestoreData]
    public class ChallengeCompletadoFirebase
    {
        [FirestoreProperty]
        public long UserId { get; set; }

        [FirestoreProperty]
        public int Xp { get; set; }
    }
}
