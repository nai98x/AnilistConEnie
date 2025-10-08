using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class Trigger
{
    [FirestoreProperty]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? Texto { get; set; }

    [FirestoreProperty]
    public string? ImageUrl { get; set; }

    [FirestoreProperty]
    public bool Activo { get; set; }

    [FirestoreProperty]
    public int Tipo { get; set; }
}
