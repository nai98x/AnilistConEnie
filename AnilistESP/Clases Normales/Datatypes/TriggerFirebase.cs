using Google.Cloud.Firestore;
using Google.Type;

namespace AnilistESP
{
    [FirestoreData]
    public class TriggerFirebase
    {
        [FirestoreProperty]
        public string Nombre { get; set; }

        [FirestoreProperty]
        public string? Texto { get; set; }

        [FirestoreProperty]
        public string? ImageUrl { get; set; }

        [FirestoreProperty]
        public bool Activo { get; set; }

        [FirestoreProperty]
        public int Tipo { get; set; }
    }
}
