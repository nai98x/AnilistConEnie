using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class ImagenesRepository
{
    public static async Task<List<Imagen>> GetImagenesAsync(string categoria)
    {
        var ret = new List<Imagen>();
        FirestoreDb db = FirebaseHelper.GetFirestoreClientYumiko();

        var col = db.Collection("Galeria").Document("nsfw").Collection(categoria);
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<Imagen>());
            }
        }

        return ret;
    }
}
