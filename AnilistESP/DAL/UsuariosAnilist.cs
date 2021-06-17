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

        public async Task SetAnilist(CommandContext ctx, string anilistUrl, ulong messageId, ulong userId)
        {
            FirestoreDb db = funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("Anilist").Document($"{ctx.Guild.Id}").Collection("Usuarios").Document($"{userId}");
            var snap = await doc.GetSnapshotAsync();
            UsuarioAnilistFirebase registro;
            if (snap.Exists)
            {
                registro = snap.ConvertTo<UsuarioAnilistFirebase>();
                var oldMessageId = registro.MessageId;

                registro.AnilistURL = anilistUrl;
                registro.MessageId = (long)messageId;
                registro.UserId = (long)userId;
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"AnilistURL", registro.AnilistURL},
                    {"MessageId", registro.MessageId},
                    {"UserId", registro.UserId},
                };
                await doc.UpdateAsync(data);
                await funciones.BorrarMensajeUsuarioAnilist(ctx.Client, oldMessageId);
            }
            else
            {
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"AnilistURL", anilistUrl},
                    {"MessageId", messageId},
                    {"UserId", userId},
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