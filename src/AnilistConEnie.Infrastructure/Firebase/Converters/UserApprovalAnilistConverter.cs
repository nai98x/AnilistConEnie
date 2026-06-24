using AnilistConEnie.Model.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase.Converters;

public sealed class UserApprovalAnilistConverter : IFirestoreConverter<UserApprovalAnilist>
{
    public UserApprovalAnilist FromFirestore(object value)
    {
        var map = (IDictionary<string, object>)value;
        return new UserApprovalAnilist
        {
            IdDiscord = FirestoreValue.GetLong(map, "IdDiscord"),
            IdAnilist = FirestoreValue.GetInt(map, "IdAnilist"),
            Avatar = FirestoreValue.GetString(map, "Avatar"),
            SiteUrl = FirestoreValue.GetString(map, "SiteUrl"),
            Name = FirestoreValue.GetString(map, "Name"),
            Banner = FirestoreValue.GetString(map, "Banner"),
        };
    }

    public object ToFirestore(UserApprovalAnilist value) => new Dictionary<string, object>
    {
        ["IdDiscord"] = value.IdDiscord,
        ["IdAnilist"] = value.IdAnilist,
        ["Avatar"] = value.Avatar,
        ["SiteUrl"] = value.SiteUrl,
        ["Name"] = value.Name,
        ["Banner"] = value.Banner,
    };
}
