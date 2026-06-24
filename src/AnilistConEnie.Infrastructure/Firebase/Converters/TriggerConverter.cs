using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class TriggerConverter : IFirestoreConverter<Trigger>
{
    public Trigger FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new Trigger
        {
            Nombre = FirestoreValue.GetString(map, "Nombre"),
            Texto = FirestoreValue.GetString(map, "Texto"),
            ImageUrl = FirestoreValue.GetString(map, "ImageUrl"),
            Tipo = FirestoreValue.GetInt(map, "Tipo"),
        };
    }

    public object ToFirestore(Trigger value) => new Dictionary<string, object>
    {
        ["Nombre"] = value.Nombre,
        ["Texto"] = value.Texto,
        ["ImageUrl"] = value.ImageUrl,
        ["Tipo"] = value.Tipo,
    };
}
