using DSharpPlus;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class UsuariosAnilist
    {
        public async Task<List<UsuarioAnilistFirebase>> GetListaUsuarios()
        {
            var ret = new List<UsuarioAnilistFirebase>();
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();

            CollectionReference col = db.Collection("Anilist");
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

        public async Task<UsuarioAnilistFirebase> GetPerfil(ulong userId)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
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

        public async Task SetAnilist(DiscordClient client, DiscordGuild guild, string anilistUrl, DiscordMember miembro)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Anilist").Document($"{miembro.Id}");
            var snap = await doc.GetSnapshotAsync();
            UsuarioAnilistFirebase registro;
            DiscordChannel channel = await Funciones.GetCanalUsuariosAnilist(client, guild);
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

        public async Task DeleteAnilist(ulong userId)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                await doc.DeleteAsync();
            }
        }

        // LISTA DE YUMIKO

        public async Task SetAnilistYumiko(int anilistId, ulong userId)
        {
            FirestoreDb db = Funciones.GetFirestoreClientYumiko();
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