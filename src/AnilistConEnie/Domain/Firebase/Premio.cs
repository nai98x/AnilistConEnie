using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class Premio
{
    [FirestoreProperty]
    public string Link { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty]
    public int Year { get; set; }

    [FirestoreProperty]
    public int Order { get; set; }
}
