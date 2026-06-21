using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class TriggersRepository(FirebaseService firebase) : ITriggersRepository
{
    public async Task<List<Trigger>> GetTriggers(bool enabled)
    {
        List<Trigger> ret = [];
        FirestoreDb db = await firebase.GetAnilistConEnie();

        Query? query = db.Collection("Triggers").WhereEqualTo("Activo", enabled);
        QuerySnapshot? snap = await query.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            ret.AddRange(snap.Documents.Select(document => document.ConvertTo<Trigger>()));
        }

        ret.Sort((x, y) => string.Compare(x.Nombre, y.Nombre, StringComparison.Ordinal));

        return ret;
    }

    public async Task SetTrigger(Trigger trigger)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Triggers").Document($"{trigger.Nombre}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        if (snap.Exists)
        {
            Trigger registro = snap.ConvertTo<Trigger>();

            Dictionary<string, object> data = new()
            {
                { "Nombre", registro.Nombre },
                { "Texto", trigger.Texto },
                { "ImageUrl", trigger.ImageUrl },
                { "Activo", registro.Activo },
                { "Tipo", trigger.Tipo },
            };
            await doc.UpdateAsync(data);
        }
        else
        {
            Dictionary<string, object> data = new()
            {
                { "Nombre", trigger.Nombre.ToLower() },
                { "Texto", trigger.Texto },
                { "ImageUrl", trigger.ImageUrl },
                { "Activo", true },
                { "Tipo", trigger.Tipo },
            };
            await doc.SetAsync(data);
        }
    }

    public async Task<bool> DeleteTrigger(string triggerName)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Triggers").Document($"{triggerName}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        if (!snap.Exists) return false;

        await doc.DeleteAsync();

        return true;
    }

    public async Task<Trigger?> EnableTrigger(string triggerName)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Triggers").Document($"{triggerName}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        if (!snap.Exists) return null;

        Trigger registro = snap.ConvertTo<Trigger>();

        if (registro.Activo) return null;

        Dictionary<string, object> data = new()
        {
            { "Nombre", registro.Nombre },
            { "Texto", registro.Texto },
            { "ImageUrl", registro.ImageUrl },
            { "Activo", true },
            { "Tipo", registro.Tipo },
        };

        await doc.UpdateAsync(data);

        return registro;
    }
}
