using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;

namespace AnilistConEnie.Infrastructure.Repositories;

// Única superficie Firebase del bot: el espejo del vínculo en Yumiko (Firestore externa) y la subida
// de imágenes a Storage (/subirimagen).
public class FirebaseRepository(FirebaseService firebase) : IFirebaseRepository
{
    public Task SetAnilistYumiko(int anilistId, ulong userId)
    {
        DocumentReference doc = firebase.GetYumiko().Collection("AnilistUsers").Document($"{userId}");

        Dictionary<string, object> data = new()
        {
            { "AnilistId", anilistId },
            { "UserId", (long)userId },
        };

        return doc.SetAsync(data, SetOptions.MergeAll);
    }

    public async Task<string> UploadImageAsync(Stream stream, string fileName, ulong userId)
    {
        (StorageClient client, string bucket) = await firebase.GetAnilistConEnieStorageAsync();

        string objectName = $"ImagenesUsuarios/{userId}/{fileName}.png";
        string token = Guid.NewGuid().ToString();

        Google.Apis.Storage.v1.Data.Object metadata = new()
        {
            Bucket = bucket,
            Name = objectName,
            ContentType = "image/png",
            Metadata = new Dictionary<string, string> { ["firebaseStorageDownloadTokens"] = token }
        };

        await client.UploadObjectAsync(metadata, stream);

        string encodedPath = Uri.EscapeDataString(objectName);
        return $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{encodedPath}?alt=media&token={token}";
    }
}
