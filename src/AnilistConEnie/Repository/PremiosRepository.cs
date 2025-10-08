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
        List<Premio> ret = [];

        CollectionReference col = db.Collection("Premios");
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            ret.AddRange(snap.Documents.Select(document => document.ConvertTo<Premio>()));
        }

        return ret;
    }

    public static async Task SetPremio(int anio, Season season, string link)
    {
        string nombre = $"{season.GetName()} {anio}";

        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Premios").Document(nombre);
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

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
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }
}
