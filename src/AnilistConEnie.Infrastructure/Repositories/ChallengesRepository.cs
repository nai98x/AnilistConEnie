using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class ChallengesRepository(FirebaseService firebase) : IChallengesRepository
{
    public async Task<List<Challenge>> GetLista()
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<Challenge> ret = [];

        CollectionReference col = db.Collection("Challenges");
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count <= 0) return ret;
        ret.AddRange(snap.Documents.Select(document => document.ConvertTo<Challenge>()));

        return ret;
    }

    public async Task<List<ChallengeCompletado>> GetRankingUsuarios()
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<ChallengeCompletado> ret = [];

        CollectionReference col = db.Collection("Challenges");
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (DocumentSnapshot? document in snap.Documents)
            {
                Challenge? challenge = document.ConvertTo<Challenge>();
                CollectionReference col2 = db.Collection("Challenges").Document(challenge.Nombre).Collection("Usuarios");
                QuerySnapshot? snap2 = await col2.GetSnapshotAsync();

                if (snap2.Count <= 0) continue;
                foreach (DocumentSnapshot? document2 in snap2.Documents)
                {
                    ChallengeCompletado? registro = document2.ConvertTo<ChallengeCompletado>();

                    ChallengeCompletado? registroExistente = ret.Find(x => x.UserId == registro.UserId);
                    if (registroExistente != null)
                    {
                        registroExistente.Xp += registro.Xp;
                    }
                    else
                    {
                        ret.Add(registro);
                    }
                }
            }
        }

        ret.Sort((x, y) => y.Xp.CompareTo(x.Xp));
        return ret;
    }

    public async Task Set(string nombre, string link, bool disponible, DateTime? vencimiento)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Challenges").Document($"{nombre}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            Dictionary<string, object?> data = new()
            {
                { "Nombre", nombre },
                { "Link", link },
                { "Disponible", disponible },
                { "Vencimiento", vencimiento }
            };
            await doc.SetAsync(data);
        }
        else
        {
            Challenge? registro = snap.ConvertTo<Challenge>();
            registro.Link = link;
            Dictionary<string, object?> data = new()
            {
                { "Nombre", registro.Nombre },
                { "Link", registro.Link },
                { "Disponible", disponible },
                { "Vencimiento", vencimiento }
            };
            await doc.UpdateAsync(data);
        }
    }

    public async Task SetUsuarioChallenge(string nombreChallenge, long userId, int xp, DateTimeOffset offset)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Challenges").Document($"{nombreChallenge}").Collection("Usuarios").Document($"{userId}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            Dictionary<string, object> data = new()
            {
                { "UserId", userId },
                { "Xp", xp },
                { "Date", offset }
            };
            await doc.SetAsync(data);
        }
        else
        {
            ChallengeCompletado? registro = snap.ConvertTo<ChallengeCompletado>();
            registro.Xp = xp;
            registro.Date = offset;
            Dictionary<string, object> data = new()
            {
                { "UserId", registro.UserId },
                { "Xp", registro.Xp },
                { "Date", registro.Date }
            };
            await doc.UpdateAsync(data);
        }
    }

    public async Task<List<ChallengeCompletado>> GetListaUsuariosCompletaron(string nombreChallenge)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<ChallengeCompletado> ret = [];

        CollectionReference col = db.Collection("Challenges").Document($"{nombreChallenge}").Collection("Usuarios");
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            ret.AddRange(snap.Documents.Select(document => document.ConvertTo<ChallengeCompletado>()));
        }

        return ret;
    }

    public async Task<List<UsuarioChallenge>> GetChallengesUsuario(ulong userId)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<UsuarioChallenge> ret = [];

        CollectionReference colChallenges = db.Collection("Challenges");
        IAsyncEnumerable<DocumentReference> subcollectionsChallenges = colChallenges.ListDocumentsAsync();
        IAsyncEnumerator<DocumentReference> subcollectionsEnumerator = subcollectionsChallenges.GetAsyncEnumerator(default);
        while (await subcollectionsEnumerator.MoveNextAsync())
        {
            DocumentReference subcollectionRef = subcollectionsEnumerator.Current;
            DocumentSnapshot challengeSnap = await subcollectionRef.GetSnapshotAsync();
            if (!challengeSnap.Exists) continue;

            Challenge challenge = challengeSnap.ConvertTo<Challenge>();
            DocumentReference doc = db.Collection("Challenges").Document(subcollectionRef.Id).Collection("Usuarios").Document($"{userId}");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists) continue;

            ChallengeCompletado? registro = snap.ConvertTo<ChallengeCompletado>();
            ret.Add(new UsuarioChallenge
            {
                UserId = registro.UserId,
                Xp = registro.Xp,
                Challenge = challenge
            });
        }

        return ret;
    }

    public async Task<List<UsuarioChallenge>> GetChallengesUsuariosNoDelServer(HashSet<ulong> memberIds)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<UsuarioChallenge> ret = [];

        CollectionReference colChallenges = db.Collection("Challenges");
        IAsyncEnumerable<DocumentReference> subcollectionsChallenges = colChallenges.ListDocumentsAsync();
        IAsyncEnumerator<DocumentReference> subcollectionsEnumerator = subcollectionsChallenges.GetAsyncEnumerator(default);
        while (await subcollectionsEnumerator.MoveNextAsync())
        {
            DocumentReference subcollectionRef = subcollectionsEnumerator.Current;
            DocumentSnapshot challengeSnap = await subcollectionRef.GetSnapshotAsync();
            if (!challengeSnap.Exists) continue;

            Challenge challenge = challengeSnap.ConvertTo<Challenge>();
            CollectionReference colChallengesUsr = db.Collection("Challenges").Document(subcollectionRef.Id).Collection("Usuarios");
            IAsyncEnumerable<DocumentReference> subcollectionsChallengesUsr = colChallengesUsr.ListDocumentsAsync();
            IAsyncEnumerator<DocumentReference> subcollectionsEnumeratorUsr = subcollectionsChallengesUsr.GetAsyncEnumerator(default);
            while (await subcollectionsEnumeratorUsr.MoveNextAsync())
            {
                DocumentReference subcollectionRefUsr = subcollectionsEnumeratorUsr.Current;
                if (memberIds.Contains(ulong.Parse(subcollectionRefUsr.Id))) continue;

                DocumentSnapshot snap = await subcollectionRefUsr.GetSnapshotAsync();
                if (!snap.Exists) continue;

                ChallengeCompletado? registro = snap.ConvertTo<ChallengeCompletado>();
                ret.Add(new UsuarioChallenge
                {
                    UserId = registro.UserId,
                    Xp = registro.Xp,
                    Challenge = challenge
                });
            }
        }

        return ret;
    }
}
