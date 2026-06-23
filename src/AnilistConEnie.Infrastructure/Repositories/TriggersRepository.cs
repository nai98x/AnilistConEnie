using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class TriggersRepository(FirebaseService firebase) : FirestoreRepository(firebase), ITriggersRepository
{
    public async Task<List<Trigger>> GetTriggers()
    {
        FirestoreDb db = await GetDbAsync();
        Query query = db.Collection("Triggers");
        List<Trigger> ret = await QueryListAsync<Trigger>(query);

        ret.Sort((x, y) => string.Compare(x.Nombre, y.Nombre, StringComparison.Ordinal));

        return ret;
    }

    public async Task SetTrigger(Trigger trigger)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Triggers").Document($"{trigger.Nombre}");

        Dictionary<string, object> data = new()
        {
            { "Nombre", trigger.Nombre.ToLower() },
            { "Texto", trigger.Texto },
            { "ImageUrl", trigger.ImageUrl },
            { "Tipo", trigger.Tipo },
        };

        await doc.SetAsync(data);
    }

    public async Task<bool> DeleteTrigger(string triggerName)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Triggers").Document($"{triggerName}");
        DocumentSnapshot snap = await doc.GetSnapshotAsync();

        if (!snap.Exists) return false;

        await doc.DeleteAsync();

        return true;
    }
}
