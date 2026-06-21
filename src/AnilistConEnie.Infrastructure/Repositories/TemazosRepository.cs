using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class TemazosRepository(FirebaseService firebase) : ITemazosRepository
{
    public async Task<List<Temazo>> GetTemazosByUser(ulong userId, int edicion)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<Temazo> ret = [];

        CollectionReference? query = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes").Document($"{userId}").Collection("Slots");
        QuerySnapshot? snap = await query.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            ret.AddRange(snap.Documents.Select(document => document.ConvertTo<Temazo>()));
        }

        return ret;
    }

    public async Task<List<Temazo>> GetTemazos(int edicion)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<Temazo> ret = [];

        CollectionReference? query = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes");
        IAsyncEnumerable<DocumentReference> subcollectionsParticipantes = query.ListDocumentsAsync();
        IAsyncEnumerator<DocumentReference> subcollectionsEnumerator = subcollectionsParticipantes.GetAsyncEnumerator(default);
        while (await subcollectionsEnumerator.MoveNextAsync())
        {
            DocumentReference subcollectionRef = subcollectionsEnumerator.Current;
            query = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes").Document($"{subcollectionRef.Id}").Collection("Slots");
            QuerySnapshot? snap = await query.GetSnapshotAsync();
            if (snap.Count > 0)
            {
                ret.AddRange(snap.Documents.Select(document => document.ConvertTo<Temazo>()));
            }
        }

        ret.Shuffle();

        return ret;
    }

    public async Task<List<Temazo>> GetTemazosByVotes(int edicion)
    {
        List<Temazo> ret = await GetTemazos(edicion);

        ret = ret.Where(x => x.Puesto != 0).ToList();

        ret.Sort((x, y) => y.Puesto.CompareTo(x.Puesto));

        return ret;
    }

    public async Task SetTemazo(ulong userId, int edicion, string nombre, int slot)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes").Document($"{userId}").Collection("Slots").Document($"{slot}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        Dictionary<string, object> data = new()
        {
            { "UserId", userId },
            { "Edicion", edicion },
            { "Nombre", nombre },
            { "Slot", slot },
            { "Votos", 0 },
            { "Puesto", 0 }
        };

        if (snap.Exists)
            await doc.UpdateAsync(data);
        else
            await doc.SetAsync(data);
    }

    public async Task SaveTemazoToDisk(string url, ulong userId, int slot)
    {
        HttpClient client = new();

        byte[] bytes = await client.GetByteArrayAsync(url);

        string folderPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Temazos");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        folderPath = Path.Join(folderPath, $"{userId}");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, $"{slot}.mp3");

        await using FileStream fs = new(filePath, FileMode.Create, FileAccess.ReadWrite);
        fs.Write(bytes, 0, bytes.Length);
    }

    public MemoryStream? GetTemazoFromDisk(ulong userId, int slot)
    {
        string folderPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Temazos");
        if (!Directory.Exists(folderPath))
        {
            return null;
        }

        folderPath = Path.Join(folderPath, $"{userId}");
        if (!Directory.Exists(folderPath))
        {
            return null;
        }

        string filePath = Path.Join(folderPath, $"{slot}.mp3");
        if (!File.Exists(filePath))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(filePath);

        return bytes.ToMemoryStream();
    }

    public async Task<bool> SetVote(ulong userWhoIsVoting, ulong userId, int edicion, int slot)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();

        DocumentReference doc = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes").Document($"{userId}").Collection("Slots").Document($"{slot}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        if (!snap.Exists) return false;

        Temazo? temazo = snap.ConvertTo<Temazo>();

        DocumentReference? doc2 = db.Collection("TemazosVotos").Document($"{edicion}").Collection("Usuarios").Document($"{userWhoIsVoting}").Collection("Votos").Document($"{userId}-{slot}");
        DocumentSnapshot? snap2 = await doc2.GetSnapshotAsync();

        if (snap2.Exists) return false;

        Dictionary<string, object> data = new()
        {
            { "UserId", temazo.UserId },
            { "Edicion", temazo.Edicion },
            { "Nombre", temazo.Nombre },
            { "Slot", temazo.Slot },
            { "Votos", temazo.Votos + 1 },
        };

        await doc.UpdateAsync(data);

        Dictionary<string, object> voteData = new()
        {
            { "UserId", userId },
            { "UserToVoteId", userWhoIsVoting }
        };

        await doc2.SetAsync(voteData);

        return true;
    }
}
