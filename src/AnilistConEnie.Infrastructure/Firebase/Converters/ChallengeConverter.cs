using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class ChallengeConverter : IFirestoreConverter<Challenge>
{
    public Challenge FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new Challenge
        {
            Nombre = FirestoreValue.GetString(map, "Nombre"),
            Link = FirestoreValue.GetString(map, "Link"),
            Disponible = FirestoreValue.GetBool(map, "Disponible"),
            Vencimiento = FirestoreValue.GetNullableDateTime(map, "Vencimiento"),
        };
    }

    public object ToFirestore(Challenge value)
    {
        Dictionary<string, object?> data = new()
        {
            ["Nombre"] = value.Nombre,
            ["Link"] = value.Link,
            ["Disponible"] = value.Disponible,
            ["Vencimiento"] = value.Vencimiento,
        };
        return data;
    }
}
