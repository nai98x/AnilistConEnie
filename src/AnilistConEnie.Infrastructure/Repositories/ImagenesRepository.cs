using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class ImagenesRepository(FirebaseService firebase) : FirestoreRepository(firebase), IImagenesRepository
{
    public Task<List<Imagen>> GetImagenesAsync(string categoria)
    {
        CollectionReference col = GetYumikoDb().Collection("Galeria").Document("nsfw").Collection(categoria);
        return QueryListAsync<Imagen>(col);
    }
}
