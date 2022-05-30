using Google.Cloud.Firestore;

namespace AnilistESP
{
    [FirestoreData]
    public class IntercambiosSettingsFirebase
    {
        [FirestoreProperty]
        public bool Iniciado { get; set; }

        [FirestoreProperty]
        public bool Inscripciones { get; set; }

        [FirestoreProperty]
        public bool Elecciones { get; set; }

        [FirestoreProperty]
        public long ChannelId { get; set; }

        [FirestoreProperty]
        public long MessageInscriptosId { get; set; }

        [FirestoreProperty]
        public long MessageRecomendacionesId { get; set; }
    }
}
