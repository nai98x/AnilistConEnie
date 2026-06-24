using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class UsuarioAnilistYumikoConverter : IFirestoreConverter<UsuarioAnilistYumiko>
{
    public UsuarioAnilistYumiko FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new UsuarioAnilistYumiko
        {
            AnilistId = FirestoreValue.GetInt(map, "AnilistId"),
            UserId = FirestoreValue.GetLong(map, "UserId"),
        };
    }

    public object ToFirestore(UsuarioAnilistYumiko value) => new Dictionary<string, object>
    {
        ["AnilistId"] = value.AnilistId,
        ["UserId"] = value.UserId,
    };
}
