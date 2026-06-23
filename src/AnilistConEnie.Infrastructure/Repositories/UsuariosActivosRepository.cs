using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class UsuariosActivosRepository(FirebaseService firebase) : FirestoreRepository(firebase), IUsuariosActivosRepository
{
    public async Task<List<UsuarioActivo>> GetUsuariosActivos(HashSet<ulong> memberIds)
    {
        FirestoreDb db = await GetDbAsync();
        DateTime date = new(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);

        Query query = db.Collection("ActividadUsuarios").WhereGreaterThan("LastActivity", date.AddMonths(-3));
        List<UsuarioActivo> usuarios = await QueryListAsync<UsuarioActivo>(query);

        return usuarios.Where(x => memberIds.Contains((ulong)x.UserId)).ToList();
    }

    public async Task<List<UsuarioActivo>> GetUsuariosInactivos(HashSet<ulong> memberIds, int months)
    {
        FirestoreDb db = await GetDbAsync();
        DateTime date = new(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);

        Query query = db.Collection("ActividadUsuarios").WhereLessThan("LastActivity", date.AddMonths(-months));
        List<UsuarioActivo> usuarios = await QueryListAsync<UsuarioActivo>(query);

        return usuarios.Where(x => memberIds.Contains((ulong)x.UserId)).ToList();
    }

    public async Task SetUsuarioActividad(long userId)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("ActividadUsuarios").Document($"{userId}");

        Dictionary<string, object> data = new()
        {
            { "UserId", userId },
            { "LastActivity", new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc) }
        };

        await UpsertAsync(doc, data);
    }
}
