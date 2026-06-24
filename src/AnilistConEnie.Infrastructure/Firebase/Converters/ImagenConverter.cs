using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class ImagenConverter : IFirestoreConverter<Imagen>
{
    public Imagen FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new Imagen
        {
            ImageUrl = FirestoreValue.GetString(map, "imageUrl"),
        };
    }

    public object ToFirestore(Imagen value) => new Dictionary<string, object>
    {
        ["imageUrl"] = value.ImageUrl,
    };
}
