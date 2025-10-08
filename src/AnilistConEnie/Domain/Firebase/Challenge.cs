using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class Challenge
{
    [FirestoreProperty]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Link { get; set; } = string.Empty;

    [FirestoreProperty]
    public bool Disponible { get; set; }

    [FirestoreProperty]
    public System.DateTime? Vencimiento { get; set; }
}
