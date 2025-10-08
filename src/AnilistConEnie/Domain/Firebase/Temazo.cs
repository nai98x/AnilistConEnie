using Google.Cloud.Firestore;

namespace AnilistConEnie.Domain.Firebase;

[FirestoreData]
public class Temazo
{
    [FirestoreProperty]
    public long UserId { get; set; }

    [FirestoreProperty]
    public int Edicion { get; set; }

    [FirestoreProperty]
    public string Nombre { get; set; } = string.Empty;

    [FirestoreProperty]
    public int Slot { get; set; }

    [FirestoreProperty]
    public int Votos { get; set; }

    [FirestoreProperty]
    public int Puesto { get; set; }
}
