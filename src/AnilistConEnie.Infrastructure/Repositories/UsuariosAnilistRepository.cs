using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class UsuariosAnilistRepository(FirebaseService firebase) : FirestoreRepository(firebase), IUsuariosAnilistRepository
{
    public async Task<List<UsuarioAnilist>> GetListaUsuarios()
    {
        FirestoreDb db = await GetDbAsync();
        return await QueryListAsync<UsuarioAnilist>(db.Collection("Anilist"));
    }

    public async Task<UsuarioAnilist?> GetPerfil(ulong userId)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
        return await GetDocumentAsync<UsuarioAnilist>(doc);
    }

    public async Task SetAnilist(ulong userId, string anilistUrl, long messageId)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Anilist").Document($"{userId}");

        Dictionary<string, object> data = new()
        {
            { "AnilistURL", anilistUrl },
            { "MessageId", messageId },
            { "UserId", (long)userId },
        };

        await UpsertAsync(doc, data);
    }

    public async Task DeleteAnilist(ulong userId)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Anilist").Document($"{userId}");
        await DeleteIfExistsAsync(doc);
    }

    public async Task<List<UsuarioAnilistBaneado>> GetListaUsuariosBaneados()
    {
        FirestoreDb db = await GetDbAsync();
        return await QueryListAsync<UsuarioAnilistBaneado>(db.Collection("AnilistBaneados"));
    }

    public async Task DeleteUsuarioBaneado(int userId)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("AnilistBaneados").Document($"{userId}");
        await DeleteIfExistsAsync(doc);
    }

    public async Task AgregarUsuarioBaneado(int userId)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("AnilistBaneados").Document($"{userId}");

        Dictionary<string, object> data = new()
        {
            { "AnilistUserId", userId }
        };

        await SetIfAbsentAsync(doc, data);
    }

    public async Task AgregarUsuarioApproval(UserApprovalAnilist user)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("AnilistApproval").Document($"{user.IdDiscord}");
        await SetIfAbsentAsync(doc, user);
    }

    public async Task EliminarUsuarioApproval(long userIdDiscord)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("AnilistApproval").Document($"{userIdDiscord}");
        await DeleteIfExistsAsync(doc);
    }

    public async Task<UserApprovalAnilist?> GetUsuarioApproval(long userIdDiscord)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("AnilistApproval").Document($"{userIdDiscord}");
        return await GetDocumentAsync<UserApprovalAnilist>(doc);
    }

    public async Task SetAnilistYumiko(int anilistId, ulong userId)
    {
        DocumentReference doc = GetYumikoDb().Collection("AnilistUsers").Document($"{userId}");

        Dictionary<string, object> data = new()
        {
            { "AnilistId", anilistId },
            { "UserId", (long)userId },
        };

        await UpsertAsync(doc, data);
    }
}
