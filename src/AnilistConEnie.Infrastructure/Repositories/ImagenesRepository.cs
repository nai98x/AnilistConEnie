using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class ImagenesRepository(FirebaseService firebase) : IImagenesRepository
{
    public async Task<List<Imagen>> GetImagenesAsync(string categoria)
    {
        List<Imagen> ret = [];
        FirestoreDb db = firebase.GetYumiko();

        CollectionReference? col = db.Collection("Galeria").Document("nsfw").Collection(categoria);
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count <= 0) return ret;

        ret.AddRange(snap.Documents.Select(document => document.ConvertTo<Imagen>()));

        return ret;
    }
}
