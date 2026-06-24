using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class UsuarioAnilistBaneadoConverter : IFirestoreConverter<UsuarioAnilistBaneado>
{
    public UsuarioAnilistBaneado FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new UsuarioAnilistBaneado
        {
            AnilistUserId = FirestoreValue.GetInt(map, "AnilistUserId"),
        };
    }

    public object ToFirestore(UsuarioAnilistBaneado value) => new Dictionary<string, object>
    {
        ["AnilistUserId"] = value.AnilistUserId,
    };
}
