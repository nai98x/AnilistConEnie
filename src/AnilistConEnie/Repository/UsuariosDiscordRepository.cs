using AnilistConEnie.Domain.Datatypes;
using AnilistConEnie.Domain.Enum;
using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Repository;

public class UsuariosDiscordRepository
{
    public static async Task<List<UsuarioDiscord>> GetListaUsuarios()
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<UsuarioDiscord>();

        CollectionReference col = db.Collection("Cumpleaños");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<UsuarioDiscord>());
            }
        }

        return ret;
    }

    public async Task<List<UserCumple>> GetBirthdaysHoy()
    {
        List<UserCumple> lista = new();
        var listaFirebase = await GetListaUsuarios();
        listaFirebase.ForEach(x =>
        {
            var fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year);
            if (fchAux.Date == DateTime.Today.Date)
            {
                lista.Add(new UserCumple()
                {
                    Id = x.user_id,
                    Birthday = x.Birthday,
                    BirthdayActual = fchAux,
                    MostrarYear = x.MostrarYear
                });
            }
        });
        return lista;
    }

    public static async Task<List<UserCumple>> GetBirthdaysAhora()
    {
        List<UserCumple> lista = [];
        var listaFirebase = await GetListaUsuarios();
        listaFirebase.ForEach(x =>
        {
            var cumpleHoraLocalAux = x.Birthday.AddHours(-3);
            var cumpleHoraLocal = new DateTime(day: cumpleHoraLocalAux.Day, month: cumpleHoraLocalAux.Month, year: DateTime.UtcNow.Year, hour: cumpleHoraLocalAux.Hour, minute: 0, second: 0);
            var fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year);
            if (DateTime.Now >= cumpleHoraLocal && DateTime.Now <= cumpleHoraLocal.AddDays(1))
            {
                lista.Add(new UserCumple()
                {
                    Id = x.user_id,
                    Birthday = x.Birthday,
                    BirthdayActual = fchAux,
                    MostrarYear = x.MostrarYear
                });
            }
        });
        return lista;
    }

    public static async Task<List<UserCumple>> GetBirthdays(bool month)
    {
        List<UserCumple> lista = [];
        var listaFirebase = await GetListaUsuarios();
        listaFirebase.ForEach(x =>
        {
            var fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year, hour: x.Birthday.Hour, minute: 0, second: 0, kind: DateTimeKind.Utc);
            DateTime nuevoCumple;
            if (DateTime.UtcNow > new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year))
                nuevoCumple = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year + 1, hour: x.Birthday.Hour, minute: 0, second: 0, kind: DateTimeKind.Utc);
            else
                nuevoCumple = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year, hour: x.Birthday.Hour, minute: 0, second: 0, kind: DateTimeKind.Utc);

            var fchOriginal = new DateTime(year: x.AnioFechaOriginal, month: x.MesFechaOriginal, day: x.DiaFechaOriginal);
            if (month)
            {
                if (fchAux >= DateTime.UtcNow && fchAux <= DateTime.UtcNow.AddMonths(1))
                {
                    lista.Add(new UserCumple
                    {
                        Id = x.user_id,
                        Birthday = x.Birthday,
                        BirthdayActual = nuevoCumple,
                        MostrarYear = x.MostrarYear,
                        FechaOriginal = fchOriginal
                    });
                }
            }
            else
            {
                lista.Add(new UserCumple
                {
                    Id = x.user_id,
                    Birthday = x.Birthday,
                    BirthdayActual = nuevoCumple,
                    MostrarYear = x.MostrarYear,
                    FechaOriginal = fchOriginal
                });
            }
        });
        lista.Sort((x, y) => x.BirthdayActual.CompareTo(y.BirthdayActual));
        return lista;
    }

    public static async Task SetBirthday(ulong userId, DateTime fecha, bool mostrarEdad, DateTime fechaOriginal)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Cumpleaños").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
        UsuarioDiscord registro;
        var timeutc = new DateTime(day: fecha.Day, month: fecha.Month, year: fecha.Year, hour: fecha.Hour, minute: 0, second: 0, kind: DateTimeKind.Utc);
        if (snap.Exists)
        {
            registro = snap.ConvertTo<UsuarioDiscord>();
            registro.Birthday = timeutc;
            registro.MostrarYear = mostrarEdad;
            registro.DiaFechaOriginal = fechaOriginal.Day;
            registro.MesFechaOriginal = fechaOriginal.Month;
            registro.AnioFechaOriginal = fechaOriginal.Year;
            Dictionary<string, object> data = new()
                {
                    { "user_id", registro.user_id },
                    { "Birthday", registro.Birthday },
                    { "MostrarYear", registro.MostrarYear },
                    { "DiaFechaOriginal", registro.DiaFechaOriginal },
                    { "MesFechaOriginal", registro.MesFechaOriginal },
                    { "AnioFechaOriginal", registro.AnioFechaOriginal }
                };
            await doc.UpdateAsync(data);
        }
        else
        {
            Dictionary<string, object> data = new()
                {
                    { "user_id", userId },
                    { "Birthday", timeutc },
                    { "MostrarYear", mostrarEdad },
                    { "DiaFechaOriginal", fechaOriginal.Day },
                    { "MesFechaOriginal", fechaOriginal.Month },
                    { "AnioFechaOriginal", fechaOriginal.Year }
                };
            await doc.SetAsync(data);
        }
    }

    public static async Task DeleteBirthday(ulong userId)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("Cumpleaños").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
        {
            await doc.DeleteAsync();
        }
    }

    public static async Task<List<UserDailyXp>> GetDailyXpChartFromUser(ulong userId, DateRangeXp range = DateRangeXp.Completo)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        List<UserDailyXp> ret = [];

        var col = db.Collection("XpChartHistory").Document($"{userId}").Collection("History").Document("Year").Collection($"{DateTime.Now.Year}");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                var doc = document.ConvertTo<UserDailyXp>();
                switch (range)
                {
                    case DateRangeXp.Semanal:
                        if (DateTime.Now.AddDays(-7) <= doc.Date)
                        {
                            ret.Add(doc);
                        }
                        break;
                    case DateRangeXp.Mensual:
                        if (new DateTime(day: 1, month: DateTime.Now.Month, year: DateTime.Now.Year) <= doc.Date)
                        {
                            ret.Add(doc);
                        }
                        break;
                    case DateRangeXp.Anual:
                        ret.Add(doc);
                        break;
                    default:
                        // TODO: Conseguir de otros años
                        ret.Add(doc);
                        break;
                }
            }
        }

        ret = [.. ret.OrderBy(x => x.Date)];

        return ret;
    }

    public static async Task AddDailyXp(DateTime date, ulong userId, long xp)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("XpChartHistory").Document($"{userId}").Collection("History").Document("Year").Collection($"{date.Year}").Document($"{date.DayOfYear}");
        var snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            Dictionary<string, object> data = new()
                {
                    { "UserId", userId },
                    { "Xp", xp },
                    { "Date", date },
                };
            await doc.SetAsync(data);
        }
    }

    public static async Task AddOrRemoveUserXp(ulong userId, int action, int total, int booster, int challenges, int eventos, int intercambios, int otros)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();

        DocumentReference doc = db.Collection("XpUsers").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            Dictionary<string, object> data = new()
                {
                    { "UserId", userId },
                    { "Total", total },
                    { "Booster", booster },
                    { "Challenges", challenges },
                    { "Eventos", eventos },
                    { "Intercambios", intercambios },
                    { "Otros", otros }
                };

            await doc.SetAsync(data);
        }
        else
        {
            var registro = snap.ConvertTo<UserXp>();

            long total1, booster1, challenges1, eventos1, intercambios1, otros1;

            if (action == 0)
            {
                total1 = registro.Total; total1 += total;
                booster1 = registro.Booster; booster1 += booster;
                challenges1 = registro.Challenges; challenges1 += challenges;
                eventos1 = registro.Eventos; eventos1 += eventos;
                intercambios1 = registro.Intercambios; intercambios1 += intercambios;
                otros1 = registro.Otros; otros1 += otros;
            }
            else
            {
                total1 = registro.Total; total1 -= total;
                booster1 = registro.Booster; booster1 -= booster;
                challenges1 = registro.Challenges; challenges1 -= challenges;
                eventos1 = registro.Eventos; eventos1 -= eventos;
                intercambios1 = registro.Intercambios; intercambios1 -= intercambios;
                otros1 = registro.Otros; otros1 -= otros;
            }

            Dictionary<string, object> data = new()
                {
                    { "UserId", registro.UserId },
                    { "Total", total1 },
                    { "Booster", booster1 },
                    { "Challenges", challenges1 },
                    { "Eventos", eventos1 },
                    { "Intercambios", intercambios1 },
                    { "Otros", otros1 }
                };

            await doc.UpdateAsync(data);
        }
    }

    public static async Task<List<UserXp>> GetRanking()
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        List<UserXp> ret = [];

        CollectionReference col = db.Collection("XpUsers");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<UserXp>());
            }
        }

        return ret;
    }

    public static async Task SetCooldownTeiou(ulong userId)
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        DocumentReference doc = db.Collection("TeiouCooldown").Document($"Nickname").Collection("Usuarios").Document($"{userId}");
        var snap = await doc.GetSnapshotAsync();

        var date = new DateTime(day: DateTime.UtcNow.Day, month: DateTime.UtcNow.Month, year: DateTime.UtcNow.Year, hour: DateTime.UtcNow.Hour, minute: DateTime.UtcNow.Minute, second: DateTime.UtcNow.Second, kind: DateTimeKind.Utc);
        date = date.AddHours(24);

        Dictionary<string, object> data = new()
            {
                { "UserId", userId },
                { "Cooldown", date }
            };

        if (snap.Exists)
            await doc.UpdateAsync(data);
        else
            await doc.SetAsync(data);
    }

    public async Task<List<TeiouCooldownNickname>> GetListTeiouNicknameCooldown()
    {
        FirestoreDb db = await FirebaseHelper.GetFirestoreClientAnilistConEnie();
        var ret = new List<TeiouCooldownNickname>();

        CollectionReference col = db.Collection("TeiouCooldown").Document($"Nickname").Collection("Usuarios");
        var snap = await col.GetSnapshotAsync();

        if (snap.Count > 0)
        {
            foreach (var document in snap.Documents)
            {
                ret.Add(document.ConvertTo<TeiouCooldownNickname>());
            }
        }

        return ret;
    }
}
