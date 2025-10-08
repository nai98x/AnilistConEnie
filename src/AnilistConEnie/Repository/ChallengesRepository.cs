using AnilistConEnie.Domain.Datatypes;
using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class ChallengesRepository
{
    public static async Task<List<Challenge>> GetLista()
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<Challenge>();

        CollectionReference col = db.Collection("Challenges");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<Challenge>());
            }
        }

        return ret;
    }

    public static async Task<List<ChallengeCompletado>> GetRankingUsuarios()
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<ChallengeCompletado>();

        CollectionReference col = db.Collection("Challenges");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                var challenge = document.ConvertTo<Challenge>();
                CollectionReference col2 = db.Collection("Challenges").Document(challenge.Nombre).Collection("Usuarios");
                var snap2 = await col2.GetSnapshotAsync();

                if (snap2.Count > 0)
                {
                    foreach (var document2 in snap2.Documents)
                    {
                        var registro = document2.ConvertTo<ChallengeCompletado>();

                        var registroExistente = ret.Find(x => x.UserId == registro.UserId);
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
        }

        ret.Sort((x, y) => y.Xp.CompareTo(x.Xp));
        return ret;
    }

    public static async Task Set(string nombre, string link, bool disponible, DateTime? vencimiento)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Challenges").Document($"{nombre}");
        var snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            Dictionary<string, object> data = new()
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
            var registro = snap.ConvertTo<Challenge>();
            registro.Link = link;
            Dictionary<string, object> data = new()
                {
                    { "Nombre", registro.Nombre },
                    { "Link", registro.Link },
                    { "Disponible", disponible },
                    { "Vencimiento", vencimiento }
                };
            await doc.UpdateAsync(data);
        }
    }

    public static async Task SetUsuarioChallenge(string nombreChallenge, long userId, int xp, DateTimeOffset offset)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Challenges").Document($"{nombreChallenge}").Collection("Usuarios").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
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
            var registro = snap.ConvertTo<ChallengeCompletado>();
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

    public static async Task<List<ChallengeCompletado>> GetListaUsuariosCompletaron(string nombreChallenge)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<ChallengeCompletado>();

        CollectionReference col = db.Collection("Challenges").Document($"{nombreChallenge}").Collection("Usuarios");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<ChallengeCompletado>());
            }
        }

        return ret;
    }

    public static async Task<List<UsuarioChallenge>> GetChallengesUsuario(ulong userId)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<UsuarioChallenge>();

        CollectionReference colChallenges = db.Collection("Challenges");
        IAsyncEnumerable<DocumentReference> subcollectionsChallenges = colChallenges.ListDocumentsAsync();
        IAsyncEnumerator<DocumentReference> subcollectionsEnumerator = subcollectionsChallenges.GetAsyncEnumerator(default);
        while (await subcollectionsEnumerator.MoveNextAsync())
        {
            DocumentReference subcollectionRef = subcollectionsEnumerator.Current;
            DocumentSnapshot challengeSnap = await subcollectionRef.GetSnapshotAsync();
            if (challengeSnap.Exists)
            {
                Challenge challenge = challengeSnap.ConvertTo<Challenge>();
                DocumentReference doc = db.Collection("Challenges").Document(subcollectionRef.Id).Collection("Usuarios").Document($"{userId}");
                DocumentSnapshot snap = await doc.GetSnapshotAsync();
                if (snap.Exists)
                {
                    var registro = snap.ConvertTo<ChallengeCompletado>();
                    ret.Add(new()
                    {
                        UserId = registro.UserId,
                        Xp = registro.Xp,
                        Challenge = challenge
                    });
                }
            }
        }

        return ret;
    }

    public static async Task<List<UsuarioChallenge>> GetChallengesUsuariosNoDelServer(DiscordGuild guild)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<UsuarioChallenge>();

        CollectionReference colChallenges = db.Collection("Challenges");
        IAsyncEnumerable<DocumentReference> subcollectionsChallenges = colChallenges.ListDocumentsAsync();
        IAsyncEnumerator<DocumentReference> subcollectionsEnumerator = subcollectionsChallenges.GetAsyncEnumerator(default);
        while (await subcollectionsEnumerator.MoveNextAsync())
        {
            DocumentReference subcollectionRef = subcollectionsEnumerator.Current;
            DocumentSnapshot challengeSnap = await subcollectionRef.GetSnapshotAsync();
            if (challengeSnap.Exists)
            {
                Challenge challenge = challengeSnap.ConvertTo<Challenge>();

                CollectionReference colChallengesUsr = db.Collection("Challenges").Document(subcollectionRef.Id).Collection("Usuarios");
                IAsyncEnumerable<DocumentReference> subcollectionsChallengesUsr = colChallengesUsr.ListDocumentsAsync();
                IAsyncEnumerator<DocumentReference> subcollectionsEnumeratorUsr = subcollectionsChallengesUsr.GetAsyncEnumerator(default);
                while (await subcollectionsEnumeratorUsr.MoveNextAsync())
                {
                    DocumentReference subcollectionRefUsr = subcollectionsEnumeratorUsr.Current;
                    if (!guild.Members.TryGetValue(ulong.Parse(subcollectionRefUsr.Id), out _))
                    {
                        DocumentSnapshot snap = await subcollectionRefUsr.GetSnapshotAsync();
                        if (snap.Exists)
                        {
                            var registro = snap.ConvertTo<ChallengeCompletado>();
                            ret.Add(new()
                            {
                                UserId = registro.UserId,
                                Xp = registro.Xp,
                                Challenge = challenge
                            });
                        }
                    }
                }
            }
        }

        return ret;
    }
}
