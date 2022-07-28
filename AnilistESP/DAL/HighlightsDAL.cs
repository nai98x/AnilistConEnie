using DSharpPlus.SlashCommands;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class HighlightsDAL
    {
        public async Task<Dictionary<ulong, List<string>>> GetListaHighlights()
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            var ret = new Dictionary<ulong, List<string>>();

            CollectionReference col = db.Collection("Highlights");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    var doc = document.ConvertTo<HighlightFirebase>();

                    if (ret.TryGetValue((ulong)doc.UserId, out var values))
                    {
                        values.Add(doc.Highlight);
                    }
                    else
                    {
                        ret.Add((ulong)doc.UserId, new List<string>() { doc.Highlight });
                    }
                }
            }

            return ret;
        }

        public async Task SetHighlight(ulong userId, string palabra)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Highlights").Document($"{userId}-{palabra}");
            var snap = await doc.GetSnapshotAsync();
            HighlightFirebase registro;
            if (snap.Exists)
            {
                registro = snap.ConvertTo<HighlightFirebase>();
                registro.UserId = (long)userId;
                registro.Highlight = palabra;
                Dictionary<string, object> data = new()
                {
                    { "UserId", registro.UserId },
                    { "Highlight", registro.Highlight }
                };
                await doc.UpdateAsync(data);
            }
            else
            {
                Dictionary<string, object> data = new()
                {
                    { "UserId", userId },
                    { "Highlight", palabra }
                };
                await doc.SetAsync(data);
            }
        }

        public async Task RemoveHighlight(InteractionContext ctx, string palabra)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Highlights").Document($"{ctx.User.Id}-{palabra}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                await doc.DeleteAsync();
            }
        }
    }
}