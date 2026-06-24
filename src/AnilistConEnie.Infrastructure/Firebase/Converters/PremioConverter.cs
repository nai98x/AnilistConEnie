using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class PremioConverter : IFirestoreConverter<Premio>
{
    public Premio FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new Premio
        {
            Link = FirestoreValue.GetString(map, "Link"),
            Nombre = FirestoreValue.GetString(map, "Nombre"),
            Year = FirestoreValue.GetInt(map, "Year"),
            Order = FirestoreValue.GetInt(map, "Order"),
        };
    }

    public object ToFirestore(Premio value) => new Dictionary<string, object>
    {
        ["Link"] = value.Link,
        ["Nombre"] = value.Nombre,
        ["Year"] = value.Year,
        ["Order"] = value.Order,
    };
}
