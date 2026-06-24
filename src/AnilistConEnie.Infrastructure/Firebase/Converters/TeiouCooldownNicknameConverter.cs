using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class TeiouCooldownNicknameConverter : IFirestoreConverter<TeiouCooldownNickname>
{
    public TeiouCooldownNickname FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new TeiouCooldownNickname
        {
            UserId = FirestoreValue.GetLong(map, "UserId"),
            Cooldown = FirestoreValue.GetDateTime(map, "Cooldown"),
        };
    }

    public object ToFirestore(TeiouCooldownNickname value) => new Dictionary<string, object>
    {
        ["UserId"] = value.UserId,
        ["Cooldown"] = value.Cooldown,
    };
}
