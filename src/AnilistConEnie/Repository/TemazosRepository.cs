using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class TemazosRepository
{
    public static async Task<List<Temazo>> GetTemazosByUser(ulong userId, int edicion)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<Temazo>();

        var query = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes").Document($"{userId}").Collection("Slots");
        var snap = await query.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                var doc = document.ConvertTo<Temazo>();

                ret.Add(doc);
            }
        }

        return ret;
    }

    public static async Task<List<Temazo>> GetTemazos(int edicion)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<Temazo>();

        var query = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes");
        IAsyncEnumerable<DocumentReference> subcollectionsParticipantes = query.ListDocumentsAsync();
        IAsyncEnumerator<DocumentReference> subcollectionsEnumerator = subcollectionsParticipantes.GetAsyncEnumerator(default);
        while (await subcollectionsEnumerator.MoveNextAsync())
        {
            DocumentReference subcollectionRef = subcollectionsEnumerator.Current;
            query = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes").Document($"{subcollectionRef.Id}").Collection("Slots");
            var snap = await query.GetSnapshotAsync();
            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    var doc = document.ConvertTo<Temazo>();

                    ret.Add(doc);
                }
            }
        }

        ret.Shuffle();

        return ret;
    }

    public static async Task<List<Temazo>> GetTemazosByVotes(int edicion)
    {
        var ret = await GetTemazos(edicion);

        ret = ret.Where(x => x.Puesto != 0).ToList();

        ret.Sort((x, y) => y.Puesto.CompareTo(x.Puesto));

        return ret;
    }

    public static async Task SetTemazo(ulong userId, int edicion, string nombre, int slot)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes").Document($"{userId}").Collection("Slots").Document($"{slot}");
        var snap = await doc.GetSnapshotAsync();
        int votos = 0;
        int puesto = 0;

        Dictionary<string, object> data = new()
            {
                { "UserId", userId },
                { "Edicion", edicion },
                { "Nombre", nombre },
                { "Slot", slot },
                { "Votos", votos },
                { "Puesto", puesto }
            };

        if (snap.Exists)
            await doc.UpdateAsync(data);
        else
            await doc.SetAsync(data);
    }

    public static async Task SaveTemazoToDisk(DiscordAttachment temazo, ulong userId, int slot)
    {
        var client = new HttpClient();

        var bytes = await client.GetByteArrayAsync(temazo.Url);

        var folderPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Temazos");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        folderPath = Path.Join(folderPath, $"{userId}");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var filePath = Path.Combine(folderPath, $"{slot}.mp3");

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
        fs.Write(bytes, 0, bytes.Length);
    }

    public MemoryStream? GetTemazoFromDisk(ulong userId, int slot)
    {
        var folderPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Temazos");
        if (!Directory.Exists(folderPath))
        {
            return null;
        }

        folderPath = Path.Join(folderPath, $"{userId}");
        if (!Directory.Exists(folderPath))
        {
            return null;
        }

        var filePath = Path.Join(folderPath, $"{slot}.mp3");
        if (!File.Exists(filePath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(filePath);

        return bytes.ToMemoryStream();
    }

    public static async Task<bool> SetVote(ulong userWhoIsVoting, ulong userId, int edicion, int slot)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();

        DocumentReference doc = db.Collection("Temazos").Document($"{edicion}").Collection("Participantes").Document($"{userId}").Collection("Slots").Document($"{slot}");
        var snap = await doc.GetSnapshotAsync();

        if (snap.Exists)
        {
            var temazo = snap.ConvertTo<Temazo>();

            var doc2 = db.Collection("TemazosVotos").Document($"{edicion}").Collection("Usuarios").Document($"{userWhoIsVoting}").Collection("Votos").Document($"{userId}-{slot}");
            var snap2 = await doc2.GetSnapshotAsync();

            if (!snap2.Exists)
            {
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

        return false;
    }
}
