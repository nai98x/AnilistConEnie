using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class UserDailyXpConverter : IFirestoreConverter<UserDailyXp>
{
    public UserDailyXp FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new UserDailyXp
        {
            UserId = FirestoreValue.GetLong(map, "UserId"),
            Xp = FirestoreValue.GetLong(map, "Xp"),
            Date = FirestoreValue.GetDateTime(map, "Date"),
        };
    }

    public object ToFirestore(UserDailyXp value) => new Dictionary<string, object>
    {
        ["UserId"] = value.UserId,
        ["Xp"] = value.Xp,
        ["Date"] = value.Date,
    };
}
