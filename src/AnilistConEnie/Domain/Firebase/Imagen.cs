using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class Imagen
{
    [FirestoreProperty("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;
}
