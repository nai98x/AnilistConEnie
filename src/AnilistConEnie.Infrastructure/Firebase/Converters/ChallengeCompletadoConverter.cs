using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class ChallengeCompletadoConverter : IFirestoreConverter<ChallengeCompletado>
{
    public ChallengeCompletado FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new ChallengeCompletado
        {
            UserId = FirestoreValue.GetLong(map, "UserId"),
            Xp = FirestoreValue.GetInt(map, "Xp"),
            Date = FirestoreValue.GetDateTimeOffset(map, "Date"),
        };
    }

    public object ToFirestore(ChallengeCompletado value) => new Dictionary<string, object>
    {
        ["UserId"] = value.UserId,
        ["Xp"] = value.Xp,
        ["Date"] = value.Date,
    };
}
