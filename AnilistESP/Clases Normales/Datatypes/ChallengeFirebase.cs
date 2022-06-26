using Google.Cloud.Firestore;

namespace AnilistESP
{
    [FirestoreData]
    public class ChallengeFirebase
    {
        [FirestoreProperty]
        public string Nombre { get; set; }

        [FirestoreProperty]
        public string Link { get; set; }

        [FirestoreProperty]
        public bool Disponible { get; set; }
    }
}
