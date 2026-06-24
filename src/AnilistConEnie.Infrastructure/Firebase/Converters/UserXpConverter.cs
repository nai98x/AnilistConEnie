using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class UserXpConverter : IFirestoreConverter<UserXp>
{
    public UserXp FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new UserXp
        {
            UserId = FirestoreValue.GetLong(map, "UserId"),
            Total = FirestoreValue.GetLong(map, "Total"),
            Booster = FirestoreValue.GetLong(map, "Booster"),
            Challenges = FirestoreValue.GetLong(map, "Challenges"),
            Eventos = FirestoreValue.GetLong(map, "Eventos"),
            Intercambios = FirestoreValue.GetLong(map, "Intercambios"),
            Otros = FirestoreValue.GetLong(map, "Otros"),
        };
    }

    public object ToFirestore(UserXp value) => new Dictionary<string, object>
    {
        ["UserId"] = value.UserId,
        ["Total"] = value.Total,
        ["Booster"] = value.Booster,
        ["Challenges"] = value.Challenges,
        ["Eventos"] = value.Eventos,
        ["Intercambios"] = value.Intercambios,
        ["Otros"] = value.Otros,
    };
}
