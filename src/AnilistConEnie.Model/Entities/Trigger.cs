using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class Trigger
{
    [FirestoreProperty]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Texto { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ImageUrl { get; set; } = string.Empty;

    [FirestoreProperty]
    public bool Activo { get; set; }

    [FirestoreProperty]
    public int Tipo { get; set; }
}
