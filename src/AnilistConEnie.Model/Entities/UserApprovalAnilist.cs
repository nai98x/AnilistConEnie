using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class UserApprovalAnilist
{
    [FirestoreProperty]
    public long IdDiscord { get; set; }

    [FirestoreProperty]
    public int IdAnilist { get; set; }

    [FirestoreProperty]
    public string Avatar { get; set; } = string.Empty;

    [FirestoreProperty]
    public string SiteUrl { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Banner { get; set; } = string.Empty;
}
