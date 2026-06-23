using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class PremiosRepository(FirebaseService firebase) : FirestoreRepository(firebase), IPremiosRepository
{
    public async Task<List<Premio>> GetListaPremios()
    {
        FirestoreDb db = await GetDbAsync();
        return await QueryListAsync<Premio>(db.Collection("Premios"));
    }

    public async Task SetPremio(int anio, Season season, string link)
    {
        string nombre = $"{season.GetName()} {anio}";

        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Premios").Document(nombre);

        Dictionary<string, object> data = new()
        {
            { "Link", link },
            { "Nombre", nombre },
            { "Year", anio },
            { "Order", (int)season }
        };

        await UpsertAsync(doc, data);
    }

    public async Task RemovePremio(int anio, Season season)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Premios").Document($"{season.GetName()} {anio}");
        await DeleteIfExistsAsync(doc);
    }
}
