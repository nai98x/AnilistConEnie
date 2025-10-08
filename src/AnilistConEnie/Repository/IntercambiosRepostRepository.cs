using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class IntercambiosRepostRepository
{
    public static async Task<MensajeIntercambioRepost?> GetMensaje(ulong idMensajeHiloForo)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();

        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
        var snap = await doc.GetSnapshotAsync();

        if (snap.Exists)
        {
            return snap.ConvertTo<MensajeIntercambioRepost>();
        }

        return null;
    }

    public static async Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
        var snap = await doc.GetSnapshotAsync();

        Dictionary<string, object> data = new()
            {
                { "IdCanalHiloForo", idCanalHiloForo },
                { "IdMensajeHiloForo", idMensajeHiloForo },
                { "IdCanalMensajeRepost", idCanalMensajeRepost },
                { "IdMensajeRepost", idMensajeRepost }
            };

        if (snap.Exists)
            await doc.UpdateAsync(data);
        else
            await doc.SetAsync(data);
    }

    public static async Task DeleteMensaje(ulong idMensajeHiloForo)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();

        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
        var snap = await doc.GetSnapshotAsync();

        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }
}
