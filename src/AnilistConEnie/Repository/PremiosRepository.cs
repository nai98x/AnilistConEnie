using AnilistConEnie.Domain.Enum;
using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class PremiosRepository
{
    public static async Task<List<Premio>> GetListaPremios()
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<Premio>();

        CollectionReference col = db.Collection("Premios");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                var doc = document.ConvertTo<Premio>();
                ret.Add(doc);
            }
        }

        return ret;
    }

    public static async Task SetPremio(int anio, Season season, string link)
    {
        string nombre = $"{season.GetName()} {anio}";

        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Premios").Document(nombre);
        var snap = await doc.GetSnapshotAsync();

        Dictionary<string, object> data = new()
            {
                { "Link", link },
                { "Nombre", nombre },
                { "Year", anio },
                { "Order", (int)season }
            };

        if (snap.Exists)
            await doc.UpdateAsync(data);
        else
            await doc.SetAsync(data);
    }

    public static async Task RemovePremio(int anio, Season season)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Premios").Document($"{season.GetName()} {anio}");
        var snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }
}
