using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class ImagenesRepository
{
    public static async Task<List<Imagen>> GetImagenesAsync(string categoria)
    {
        List<Imagen> ret = [];
        FirestoreDb db = FirebaseHelper.GetFirestoreClientYumiko();

        CollectionReference? col = db.Collection("Galeria").Document("nsfw").Collection(categoria);
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count <= 0) return ret;

        ret.AddRange(snap.Documents.Select(document => document.ConvertTo<Imagen>()));

        return ret;
    }
}
