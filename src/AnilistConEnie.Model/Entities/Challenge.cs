using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

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
    public DateTime? Vencimiento { get; set; }
}
