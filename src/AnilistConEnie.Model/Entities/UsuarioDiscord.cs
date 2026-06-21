using Google.Cloud.Firestore;

namespace AnilistConEnie.Model.Entities;

[FirestoreData]
public class UsuarioDiscord
{
    [FirestoreProperty]
    public long user_id { get; set; }

    [FirestoreProperty]
    public DateTime Birthday { get; set; }

    [FirestoreProperty]
    public bool MostrarYear { get; set; }

    [FirestoreProperty]
    public int DiaFechaOriginal { get; set; }

    [FirestoreProperty]
    public int MesFechaOriginal { get; set; }

    [FirestoreProperty]
    public int AnioFechaOriginal { get; set; }
}
