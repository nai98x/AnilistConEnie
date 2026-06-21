using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class Imagen
{
    [FirestoreProperty("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;
}
