using Google.Cloud.Firestore;
using Google.Type;

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

        [FirestoreProperty]
        public System.DateTime? Vencimiento { get; set; }
    }
}
