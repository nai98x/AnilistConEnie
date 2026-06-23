using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class IntercambiosRepostRepository(FirebaseService firebase) : FirestoreRepository(firebase), IIntercambiosRepostRepository
{
    public async Task<MensajeIntercambioRepost?> GetMensaje(ulong idMensajeHiloForo)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
        return await GetDocumentAsync<MensajeIntercambioRepost>(doc);
    }

    public async Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");

        Dictionary<string, object> data = new()
        {
            { "IdCanalHiloForo", idCanalHiloForo },
            { "IdMensajeHiloForo", idMensajeHiloForo },
            { "IdCanalMensajeRepost", idCanalMensajeRepost },
            { "IdMensajeRepost", idMensajeRepost }
        };

        await UpsertAsync(doc, data);
    }

    public async Task DeleteMensaje(ulong idMensajeHiloForo)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("IntercambiosRepost").Document($"{idMensajeHiloForo}");
        await DeleteIfExistsAsync(doc);
    }
}
