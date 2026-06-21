using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class PremiosRepository(FirebaseService firebase) : IPremiosRepository
{
    public async Task<List<Premio>> GetListaPremios()
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<Premio> ret = [];

        CollectionReference col = db.Collection("Premios");
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            ret.AddRange(snap.Documents.Select(document => document.ConvertTo<Premio>()));
        }

        return ret;
    }

    public async Task SetPremio(int anio, Season season, string link)
    {
        string nombre = $"{season.GetName()} {anio}";

        FirestoreDb db = await firebase.GetAnilistConEnie();
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

    public async Task RemovePremio(int anio, Season season)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Premios").Document($"{season.GetName()} {anio}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }
}
