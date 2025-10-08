using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class MensajeIntercambioRepost
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
