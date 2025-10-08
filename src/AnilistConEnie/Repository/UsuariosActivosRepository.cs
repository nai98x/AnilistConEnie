using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class UsuariosActivosRepository
{
    public static async Task<List<UsuarioActivo>> GetUsuariosActivos(DiscordGuild guild)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<UsuarioActivo>();

        var date = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);

        var query = db.Collection("ActividadUsuarios").WhereGreaterThan("LastActivity", date.AddMonths(-3));
        var snap = await query.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                var doc = document.ConvertTo<UsuarioActivo>();

                if (guild.Members.TryGetValue((ulong)doc.UserId, out var member) && !member.IsBot)
                {
                    ret.Add(doc);
                }
            }
        }

        return ret;
    }

    public static async Task<List<UsuarioActivo>> GetUsuariosInactivos(DiscordGuild guild, int months)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<UsuarioActivo>();

        var date = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);

        var query = db.Collection("ActividadUsuarios").WhereLessThan("LastActivity", date.AddMonths(-months));
        var snap = await query.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                var doc = document.ConvertTo<UsuarioActivo>();

                if (guild.Members.TryGetValue((ulong)doc.UserId, out var member) && !member.IsBot)
                {
                    ret.Add(doc);
                }
            }
        }

        return ret;
    }

    public static async Task SetUsuarioActividad(long userId)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("ActividadUsuarios").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();

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
