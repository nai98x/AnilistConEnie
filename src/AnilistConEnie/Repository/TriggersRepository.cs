using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class TriggersRepository
{
    public static async Task<List<Trigger>> GetTriggers(bool enabled)
    {
        var ret = new List<Trigger>();
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();

        var query = db.Collection("Triggers").WhereEqualTo("Activo", enabled);
        var snap = await query.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<Trigger>());
            }
        }

        ret.Sort((x, y) => x.Nombre.CompareTo(y.Nombre));

        return ret;
    }

    public static async Task SetTrigger(Trigger trigger)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Triggers").Document($"{trigger.Nombre}");
        var snap = await doc.GetSnapshotAsync();

        if (snap.Exists)
        {
            Trigger registro = snap.ConvertTo<Trigger>();

            Dictionary<string, object> data = new()
                {
                    { "Nombre", registro.Nombre },
                    { "Texto",  trigger.Texto },
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

    public static async Task<bool> DeleteTrigger(string triggerName)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Triggers").Document($"{triggerName}");
        var snap = await doc.GetSnapshotAsync();

        if (snap.Exists)
        {
            await doc.DeleteAsync();

            return true;
        }

        return false;
    }

    public static async Task<Trigger?> EnableTrigger(string triggerName)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Triggers").Document($"{triggerName}");
        var snap = await doc.GetSnapshotAsync();

        if (snap.Exists)
        {
            Trigger registro = snap.ConvertTo<Trigger>();

            if (!registro.Activo)
            {
                Dictionary<string, object> data = new()
                    {
                        { "Nombre", registro.Nombre },
                        { "Texto",  registro.Texto },
                        { "ImageUrl", registro.ImageUrl },
                        { "Activo", true },
                        { "Tipo", registro.Tipo },
                    };

                await doc.UpdateAsync(data);

                return registro;
            }
        }

        return null;
    }
}
