using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class ChallengesDAL
    {
        public async Task<List<ChallengeFirebase>> GetLista()
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            var ret = new List<ChallengeFirebase>();

            CollectionReference col = db.Collection("Challenges");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<ChallengeFirebase>());
                }
            }

            return ret;
        }

        public async Task<List<ChallengeCompletadoFirebase>> GetRankingUsuarios()
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            var ret = new List<ChallengeCompletadoFirebase>();

            CollectionReference col = db.Collection("Challenges");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    var challenge = document.ConvertTo<ChallengeFirebase>();
                    CollectionReference col2 = db.Collection("Challenges").Document(challenge.Nombre).Collection("Usuarios");
                    var snap2 = await col2.GetSnapshotAsync();

                    if (snap2.Count > 0)
                    {
                        foreach (var document2 in snap2.Documents)
                        {
                            var registro = document2.ConvertTo<ChallengeCompletadoFirebase>();

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

        public async Task Set(string nombre, string link, bool disponible)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Challenges").Document($"{nombre}");
            var snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                Dictionary<string, object> data = new()
                {
                    { "Nombre", nombre },
                    { "Link", link },
                    { "Disponible", disponible }
                };
                await doc.SetAsync(data);
            }
            else
            {
                var registro = snap.ConvertTo<ChallengeFirebase>();
                registro.Link = link;
                Dictionary<string, object> data = new()
                {
                    { "Nombre", registro.Nombre },
                    { "Link", registro.Link },
                    { "Disponible", disponible }
                };
                await doc.UpdateAsync(data);
            }
        }

        public async Task SetUsuarioChallenge(string nombreChallenge, long userId, int xp)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Challenges").Document($"{nombreChallenge}").Collection("Usuarios").Document($"{userId}");
            var snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                Dictionary<string, object> data = new()
                {
                    { "UserId", userId },
                    { "Xp", xp }
                };
                await doc.SetAsync(data);
            }
            else
            {
                var registro = snap.ConvertTo<ChallengeCompletadoFirebase>();
                registro.Xp = xp;
                Dictionary<string, object> data = new()
                {
                    { "UserId", registro.UserId },
                    { "Xp", registro.Xp }
                };
                await doc.UpdateAsync(data);
            }
        }

        public async Task<List<ChallengeCompletadoFirebase>> GetListaUsuariosCompletaron(string nombreChallenge)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            var ret = new List<ChallengeCompletadoFirebase>();

            CollectionReference col = db.Collection("Challenges").Document($"{nombreChallenge}").Collection("Usuarios");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<ChallengeCompletadoFirebase>());
                }
            }

            return ret;
        }

        public async Task<List<UsuarioChallenge>> GetChallengesUsuario(ulong userId)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
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
                    ChallengeFirebase challenge = challengeSnap.ConvertTo<ChallengeFirebase>();
                    DocumentReference doc = db.Collection("Challenges").Document(subcollectionRef.Id).Collection("Usuarios").Document($"{userId}");
                    DocumentSnapshot snap = await doc.GetSnapshotAsync();
                    if (snap.Exists)
                    {
                        var registro = snap.ConvertTo<ChallengeCompletadoFirebase>();
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
    }
}