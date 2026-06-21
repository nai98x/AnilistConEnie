using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class UsuariosActivosRepository(FirebaseService firebase) : IUsuariosActivosRepository
{
    public async Task<List<UsuarioActivo>> GetUsuariosActivos(HashSet<ulong> memberIds)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<UsuarioActivo> ret = [];

        DateTime date = new(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);

        Query? query = db.Collection("ActividadUsuarios").WhereGreaterThan("LastActivity", date.AddMonths(-3));
        QuerySnapshot? snap = await query.GetSnapshotAsync();

        if (snap.Count <= 0) return ret;

        foreach (DocumentSnapshot? document in snap.Documents)
        {
            UsuarioActivo? doc = document.ConvertTo<UsuarioActivo>();

            if (memberIds.Contains((ulong)doc.UserId))
            {
                ret.Add(doc);
            }
        }

        return ret;
    }

    public async Task<List<UsuarioActivo>> GetUsuariosInactivos(HashSet<ulong> memberIds, int months)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        List<UsuarioActivo> ret = [];

        DateTime date = new(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);

        Query? query = db.Collection("ActividadUsuarios").WhereLessThan("LastActivity", date.AddMonths(-months));
        QuerySnapshot? snap = await query.GetSnapshotAsync();

        if (snap.Count <= 0) return ret;

        foreach (DocumentSnapshot? document in snap.Documents)
        {
            UsuarioActivo? doc = document.ConvertTo<UsuarioActivo>();

            if (memberIds.Contains((ulong)doc.UserId))
            {
                ret.Add(doc);
            }
        }

        return ret;
    }

    public async Task SetUsuarioActividad(long userId)
    {
        FirestoreDb db = await firebase.GetAnilistConEnie();
        DocumentReference doc = db.Collection("ActividadUsuarios").Document($"{userId}");
        DocumentSnapshot? snap = await doc.GetSnapshotAsync();

        Dictionary<string, object> data = new()
        {
            { "UserId", userId },
            { "LastActivity", new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc) }
        };

        if (snap.Exists)
            await doc.UpdateAsync(data);
        else
            await doc.SetAsync(data);
    }
}
