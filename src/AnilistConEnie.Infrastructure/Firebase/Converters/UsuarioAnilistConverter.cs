using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class UsuarioAnilistConverter : IFirestoreConverter<UsuarioAnilist>
{
    public UsuarioAnilist FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new UsuarioAnilist
        {
            AnilistURL = FirestoreValue.GetString(map, "AnilistURL"),
            MessageId = FirestoreValue.GetLong(map, "MessageId"),
            UserId = FirestoreValue.GetLong(map, "UserId"),
        };
    }

    public object ToFirestore(UsuarioAnilist value) => new Dictionary<string, object>
    {
        ["AnilistURL"] = value.AnilistURL,
        ["MessageId"] = value.MessageId,
        ["UserId"] = value.UserId,
    };
}
