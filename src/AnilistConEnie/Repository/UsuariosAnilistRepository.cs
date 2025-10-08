using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;

namespace AnilistConEnie.Repository;

public class UsuariosAnilistRepository (IConfiguration configuration)
{
    public static async Task<List<UsuarioAnilist>> GetListaUsuarios()
    {
        var ret = new List<UsuarioAnilist>();
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();

        CollectionReference col = db.Collection("Anilist");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<UsuarioAnilist>());
            }
        }

        return ret;
    }

    public static async Task<UsuarioAnilist?> GetPerfil(ulong userId)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            return snap.ConvertTo<UsuarioAnilist>();
        }
        else
        {
            return null;
        }
    }

    public async Task SetAnilist(DiscordGuild guild, string anilistUrl, DiscordMember miembro)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Anilist").Document($"{miembro.Id}");
        var snap = await doc.GetSnapshotAsync();
        UsuarioAnilist registro;
        DiscordChannel channel = guild.Channels[configuration.GetValue<ulong>("Ids:Channels:Perfiles")];
        if (snap.Exists)
        {
            registro = snap.ConvertTo<UsuarioAnilist>();
            DiscordMessage? mensaje = null;
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

    public static async Task DeleteAnilist(ulong userId)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }

    public static async Task<List<UsuarioAnilistBaneado>> GetListaUsuariosBaneados()
    {
        var ret = new List<UsuarioAnilistBaneado>();
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();

        CollectionReference col = db.Collection("AnilistBaneados");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<UsuarioAnilistBaneado>());
            }
        }

        return ret;
    }

    public static async Task DeleteUsuarioBaneado(int userId)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("AnilistBaneados").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }

    public static async Task AgregarUsuarioBaneado(int userId)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("AnilistBaneados").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            Dictionary<string, object> data = new()
                {
                    { "AnilistUserId", userId }
                };
            await doc.SetAsync(data);
        }
    }

    // LISTA DE YUMIKO

    public static async Task SetAnilistYumiko(int anilistId, ulong userId)
    {
        FirestoreDb db = FirebaseHelper.GetFirestoreClientYumiko();
        DocumentReference doc = db.Collection("AnilistUsers").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
        UsuarioAnilistYumiko registro;
        if (snap.Exists)
        {
            registro = snap.ConvertTo<UsuarioAnilistYumiko>();

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
