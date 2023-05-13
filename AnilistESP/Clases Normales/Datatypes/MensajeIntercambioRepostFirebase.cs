using Google.Cloud.Firestore;
using System;

namespace AnilistESP
{
    [FirestoreData]
    public class MensajeIntercambioRepostFirebase
    {
        [FirestoreProperty]
        public ulong IdCanalHiloForo { get; set; }

        [FirestoreProperty]
        public ulong IdMensajeHiloForo { get; set; }

        [FirestoreProperty]
        public ulong IdCanalMensajeRepost { get; set; }

        [FirestoreProperty]
        public ulong IdMensajeRepost { get; set; }
    }
}
