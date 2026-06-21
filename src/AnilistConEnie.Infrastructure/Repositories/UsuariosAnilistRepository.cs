using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class UsuariosAnilistRepository(FirebaseService firebase) : IUsuariosAnilistRepository
{
    public async Task<List<UsuarioAnilist>> GetListaUsuarios()
    {
        List<UsuarioAnilist> ret = [];
        FirestoreDb db = await firebase.GetAnilistConEnie();

        CollectionReference col = db.Collection("Anilist");
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count <= 0) return ret;

        ret.AddRange(snap.Documents.Select(document => document.ConvertTo<UsuarioAnilist>()));

        return ret;
    }

    public async Task<UsuarioAnilist?> GetPerfil(ulong userId)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        return snap.Exists ? snap.ConvertTo<UsuarioAnilist>() : null;
    }

    public async Task SetAnilist(ulong userId, string anilistUrl, long messageId)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        Dictionary<string, object> data = new()
        {
            { "AnilistURL", anilistUrl },
            { "MessageId", messageId },
            { "UserId", (long)userId },
        };

        if (snap.Exists)
            await doc.UpdateAsync(data);
        else
            await doc.SetAsync(data);
    }

    public async Task DeleteAnilist(ulong userId)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }

    public async Task<List<UsuarioAnilistBaneado>> GetListaUsuariosBaneados()
    {
        List<UsuarioAnilistBaneado> ret = [];
        FirestoreDb db = await firebase.GetAnilistConEnie();

        CollectionReference col = db.Collection("AnilistBaneados");
        QuerySnapshot? snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            ret.AddRange(snap.Documents.Select(document => document.ConvertTo<UsuarioAnilistBaneado>()));
        }

        return ret;
    }

    public async Task DeleteUsuarioBaneado(int userId)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("AnilistBaneados").Document($"{userId}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }

    public async Task AgregarUsuarioBaneado(int userId)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("AnilistBaneados").Document($"{userId}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            Dictionary<string, object> data = new()
            {
                { "AnilistUserId", userId }
            };
            await doc.SetAsync(data);
        }
    }

    public async Task AgregarUsuarioApproval(UserApprovalAnilist user)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("AnilistApproval").Document($"{user.IdDiscord}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.SetAsync(user);
        }
    }

    public async Task EliminarUsuarioApproval(long userIdDiscord)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("AnilistApproval").Document($"{userIdDiscord}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }

    public async Task<UserApprovalAnilist?> GetUsuarioApproval(long userIdDiscord)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();

        DocumentReference doc = db.Collection("AnilistApproval").Document($"{userIdDiscord}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        return snap.Exists ? snap.ConvertTo<UserApprovalAnilist>() : null;
    }

    public async Task SetAnilistYumiko(int anilistId, ulong userId)
    {
        FirestoreDb db = firebase.GetYumiko();
        DocumentReference doc = db.Collection("AnilistUsers").Document($"{userId}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            UsuarioAnilistYumiko registro = snap.ConvertTo<UsuarioAnilistYumiko>();

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
