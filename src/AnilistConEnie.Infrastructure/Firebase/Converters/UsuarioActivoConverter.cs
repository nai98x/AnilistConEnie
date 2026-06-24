using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class UsuarioActivoConverter : IFirestoreConverter<UsuarioActivo>
{
    public UsuarioActivo FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new UsuarioActivo
        {
            UserId = FirestoreValue.GetLong(map, "UserId"),
            LastActivity = FirestoreValue.GetDateTime(map, "LastActivity"),
        };
    }

    public object ToFirestore(UsuarioActivo value) => new Dictionary<string, object>
    {
        ["UserId"] = value.UserId,
        ["LastActivity"] = value.LastActivity,
    };
}
