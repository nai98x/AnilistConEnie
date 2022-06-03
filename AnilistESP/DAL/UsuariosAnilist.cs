using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class UsuariosAnilist
    {
        public async Task<List<UsuarioAnilistYumFirebase>> GetPerfilesServidor(ulong guildId)
        {
            List<UsuarioAnilistYumFirebase> ret = new();
            FirestoreDb db = Funciones.GetFirestoreClient();
            CollectionReference collection = db.Collection("AnilistUsers");
            IAsyncEnumerable<DocumentReference> subcollections = collection.ListDocumentsAsync();
            IAsyncEnumerator<DocumentReference> subcollectionsEnumerator = subcollections.GetAsyncEnumerator(default);
            while (await subcollectionsEnumerator.MoveNextAsync())
            {
                DocumentReference subcollectionRef = subcollectionsEnumerator.Current;
                DocumentSnapshot subcollectionSnapshot = await subcollectionRef.GetSnapshotAsync();
                if (subcollectionSnapshot.Exists)
                {
                    ret.Add(subcollectionSnapshot.ConvertTo<UsuarioAnilistYumFirebase>());
                }
            }

            return ret;
        }

        public async Task<List<UsuarioAnilistFirebase>> GetListaUsuarios(long guildId)
        {
            var ret = new List<UsuarioAnilistFirebase>();
            FirestoreDb db = Funciones.GetFirestoreClient();

            CollectionReference col = db.Collection("Anilist").Document($"{guildId}").Collection("Usuarios");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<UsuarioAnilistFirebase>());
                }
            }

            return ret;
        }

        public async Task<List<UsuarioAnilistFirebase>> GetPerfiles(long guildId)
        {
            return await GetListaUsuarios(guildId);
        }

        public async Task<UsuarioAnilistFirebase> GetPerfil(ulong guildId, ulong userId)
        {
            FirestoreDb db = Funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("Anilist").Document($"{guildId}").Collection("Usuarios").Document($"{userId}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                return snap.ConvertTo<UsuarioAnilistFirebase>();
            }
            else
            {
                return null;
            }
        }

        public async Task SetAnilist(Context ctx, string anilistUrl, DiscordMember miembro)
        {
            FirestoreDb db = Funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("Anilist").Document($"{ctx.Guild.Id}").Collection("Usuarios").Document($"{miembro.Id}");
            var snap = await doc.GetSnapshotAsync();
            UsuarioAnilistFirebase registro;
            DiscordChannel channel = await Funciones.GetCanalUsuariosAnilist(ctx.Client, ctx.Guild);
            if (snap.Exists)
            {
                registro = snap.ConvertTo<UsuarioAnilistFirebase>();
                DiscordMessage mensaje = null;
                try
                {
                    mensaje = await channel.GetMessageAsync((ulong)registro.MessageId);
                    await mensaje.ModifyAsync($"**Perfil de {miembro.Mention}**\n\n{anilistUrl}");
                }
                catch { }
                if (mensaje == null)
                {
                    mensaje = await channel.SendMessageAsync($"**Perfil de {miembro.Mention}**\n\n{anilistUrl}");
                }

                registro.AnilistURL = anilistUrl;
                registro.MessageId = (long)mensaje.Id;
                registro.UserId = (long)miembro.Id;
                Dictionary<string, object> data = new()
                {
                    { "AnilistURL", registro.AnilistURL },
                    { "MessageId", registro.MessageId },
                    { "UserId", registro.UserId },
                };
                await doc.UpdateAsync(data);
            }
            else
            {
                DiscordMessage mensaje = await channel.SendMessageAsync($"**Perfil de {miembro.Mention}**\n\n{anilistUrl}");
                Dictionary<string, object> data = new()
                {
                    { "AnilistURL", anilistUrl },
                    { "MessageId", mensaje.Id },
                    { "UserId", miembro.Id },
                };
                await doc.SetAsync(data);
            }
        }

        public async Task DeleteAnilist(ulong guildId, ulong userId)
        {
            FirestoreDb db = Funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("Anilist").Document($"{guildId}").Collection("Usuarios").Document($"{userId}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                await doc.DeleteAsync();
            }
        }

        // LISTA DE YUMIKO

        public async Task SetAnilistYumiko(int anilistId, ulong userId)
        {
            FirestoreDb db = Funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("AnilistUsers").Document($"{userId}");
            var snap = await doc.GetSnapshotAsync();
            UsuarioAnilistYumFirebase registro;
            if (snap.Exists)
            {
                registro = snap.ConvertTo<UsuarioAnilistYumFirebase>();

                registro.AnilistId = anilistId;
                registro.UserId = (long)userId;
                Dictionary<string, object> data = new()
                {
                    { "AnilistId", registro.AnilistId },
                    { "UserId", registro.UserId },
                };
                await doc.UpdateAsync(data);
            }
            else
            {
                Dictionary<string, object> data = new()
                {
                    { "AnilistId", anilistId },
                    { "UserId", userId },
                };
                await doc.SetAsync(data);
            }
        }
    }
}