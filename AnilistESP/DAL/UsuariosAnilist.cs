using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class UsuariosAnilist
    {
        private readonly FuncionesAuxiliares funciones = new FuncionesAuxiliares();

        public async Task<List<UsuarioAnilistFirebase>> GetListaUsuarios(long guildId)
        {
            var ret = new List<UsuarioAnilistFirebase>();
            FirestoreDb db = funciones.GetFirestoreClient();

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
            FirestoreDb db = funciones.GetFirestoreClient();
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

        public async Task SetAnilist(CommandContext ctx, string anilistUrl, DiscordMember miembro)
        {
            FirestoreDb db = funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("Anilist").Document($"{ctx.Guild.Id}").Collection("Usuarios").Document($"{miembro.Id}");
            var snap = await doc.GetSnapshotAsync();
            UsuarioAnilistFirebase registro;
            DiscordChannel channel = await funciones.GetCanalUsuariosAnilist(ctx.Client, ctx.Guild);
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
                if(mensaje == null)
                {
                    mensaje = await channel.SendMessageAsync($"**Perfil de {miembro.Mention}**\n\n{anilistUrl}");
                }

                registro.AnilistURL = anilistUrl;
                registro.MessageId = (long)mensaje.Id;
                registro.UserId = (long)miembro.Id;
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"AnilistURL", registro.AnilistURL},
                    {"MessageId", registro.MessageId},
                    {"UserId", registro.UserId},
                };
                await doc.UpdateAsync(data);
            }
            else
            {
                DiscordMessage mensaje = await channel.SendMessageAsync($"**Perfil de {miembro.Mention}**\n\n{anilistUrl}");
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"AnilistURL", anilistUrl},
                    {"MessageId", mensaje.Id},
                    {"UserId", miembro.Id},
                };
                await doc.SetAsync(data);
            }
        }

        public async Task DeleteAnilist(ulong guildId, ulong userId)
        {
            FirestoreDb db = funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("Anilist").Document($"{guildId}").Collection("Usuarios").Document($"{userId}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                await doc.DeleteAsync();
            }
        }
    }
}