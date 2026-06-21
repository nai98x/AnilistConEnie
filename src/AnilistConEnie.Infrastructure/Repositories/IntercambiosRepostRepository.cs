using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class IntercambiosRepostRepository(FirebaseService firebase) : IIntercambiosRepostRepository
{
    public async Task<MensajeIntercambioRepost?> GetMensaje(ulong idMensajeHiloForo)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();

        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        return snap.Exists ? snap.ConvertTo<MensajeIntercambioRepost>() : null;
    }

    public async Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

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

    public async Task DeleteMensaje(ulong idMensajeHiloForo)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();

        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }
}
