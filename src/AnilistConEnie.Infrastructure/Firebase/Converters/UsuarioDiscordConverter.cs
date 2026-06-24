using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class UsuarioDiscordConverter : IFirestoreConverter<UsuarioDiscord>
{
    public UsuarioDiscord FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new UsuarioDiscord
        {
            user_id = FirestoreValue.GetLong(map, "user_id"),
            Birthday = FirestoreValue.GetDateTime(map, "Birthday"),
            MostrarYear = FirestoreValue.GetBool(map, "MostrarYear"),
            DiaFechaOriginal = FirestoreValue.GetInt(map, "DiaFechaOriginal"),
            MesFechaOriginal = FirestoreValue.GetInt(map, "MesFechaOriginal"),
            AnioFechaOriginal = FirestoreValue.GetInt(map, "AnioFechaOriginal"),
        };
    }

    public object ToFirestore(UsuarioDiscord value) => new Dictionary<string, object>
    {
        ["user_id"] = value.user_id,
        ["Birthday"] = value.Birthday,
        ["MostrarYear"] = value.MostrarYear,
        ["DiaFechaOriginal"] = value.DiaFechaOriginal,
        ["MesFechaOriginal"] = value.MesFechaOriginal,
        ["AnioFechaOriginal"] = value.AnioFechaOriginal,
    };
}
