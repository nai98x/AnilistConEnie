using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public class UsuariosDiscordRepository(FirebaseService firebase) : FirestoreRepository(firebase), IUsuariosDiscordRepository
{
    public async Task<List<UsuarioDiscord>> GetListaUsuarios()
    {
        FirestoreDb db = await GetDbAsync();
        return await QueryListAsync<UsuarioDiscord>(db.Collection("Cumpleaños"));
    }

    public async Task<List<UserCumple>> GetBirthdaysHoy()
    {
        List<UserCumple> lista = [];
        List<UsuarioDiscord> listaFirebase = await GetListaUsuarios();
        listaFirebase.ForEach(x =>
        {
            DateTime fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year);
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

    public async Task<List<UserCumple>> GetBirthdaysAhora()
    {
        List<UserCumple> lista = [];
        List<UsuarioDiscord> listaFirebase = await GetListaUsuarios();
        listaFirebase.ForEach(x =>
        {
            DateTime cumpleHoraLocalAux = x.Birthday.AddHours(-3);
            DateTime cumpleHoraLocal = new DateTime(day: cumpleHoraLocalAux.Day, month: cumpleHoraLocalAux.Month, year: DateTime.UtcNow.Year, hour: cumpleHoraLocalAux.Hour, minute: 0, second: 0);
            DateTime fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year);
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

    public async Task<List<UserCumple>> GetBirthdays(bool month)
    {
        List<UserCumple> lista = [];
        List<UsuarioDiscord> listaFirebase = await GetListaUsuarios();
        listaFirebase.ForEach(x =>
        {
            DateTime fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year, hour: x.Birthday.Hour, minute: 0, second: 0, kind: DateTimeKind.Utc);
            DateTime nuevoCumple = DateTime.UtcNow > new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year) ? new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year + 1, hour: x.Birthday.Hour, minute: 0, second: 0, kind: DateTimeKind.Utc) : new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.UtcNow.Year, hour: x.Birthday.Hour, minute: 0, second: 0, kind: DateTimeKind.Utc);

            DateTime fchOriginal = new DateTime(year: x.AnioFechaOriginal, month: x.MesFechaOriginal, day: x.DiaFechaOriginal);
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

    public async Task SetBirthday(ulong userId, DateTime fecha, bool mostrarEdad, DateTime fechaOriginal)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Cumpleaños").Document($"{userId}");
        DateTime timeUtc = new(day: fecha.Day, month: fecha.Month, year: fecha.Year, hour: fecha.Hour, minute: 0, second: 0, kind: DateTimeKind.Utc);

        Dictionary<string, object> data = new()
        {
            { "user_id", userId },
            { "Birthday", timeUtc },
            { "MostrarYear", mostrarEdad },
            { "DiaFechaOriginal", fechaOriginal.Day },
            { "MesFechaOriginal", fechaOriginal.Month },
            { "AnioFechaOriginal", fechaOriginal.Year }
        };

        await UpsertAsync(doc, data);
    }

    public async Task DeleteBirthday(ulong userId)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("Cumpleaños").Document($"{userId}");
        await DeleteIfExistsAsync(doc);
    }

    public async Task<List<UserDailyXp>> GetDailyXpChartFromUser(ulong userId, DateRangeXp range = DateRangeXp.Completo)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference yearDoc = db.Collection("XpChartHistory").Document($"{userId}").Collection("History").Document("Year");
        int currentYear = DateTime.Now.Year;
        List<UserDailyXp> ret = [];

        // Se recorren todas las colecciones por año: el año actual trae todos los registros (según el
        // rango) y los años anteriores se reducen a un registro por mes para no traer el historial completo.
        await foreach (CollectionReference yearCol in yearDoc.ListCollectionsAsync())
        {
            if (!int.TryParse(yearCol.Id, out int year))
                continue;

            if (year == currentYear)
            {
                List<UserDailyXp> all = await QueryListAsync<UserDailyXp>(yearCol);
                IEnumerable<UserDailyXp> filtered = range switch
                {
                    DateRangeXp.Semanal => all.Where(x => DateTime.Now.AddDays(-7) <= x.Date),
                    DateRangeXp.Mensual => all.Where(x => new DateTime(day: 1, month: DateTime.Now.Month, year: currentYear) <= x.Date),
                    _ => all
                };
                ret.AddRange(filtered);
            }
            else
            {
                for (int mes = 1; mes <= 12; mes++)
                {
                    DateTime desde = new(year, mes, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime hasta = mes == 12
                        ? new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        : new DateTime(year, mes + 1, 1, 0, 0, 0, DateTimeKind.Utc);

                    Query query = yearCol
                        .WhereGreaterThanOrEqualTo("Date", desde)
                        .WhereLessThan("Date", hasta)
                        .OrderBy("Date")
                        .Limit(1);

                    QuerySnapshot snap = await query.GetSnapshotAsync();
                    if (snap.Count > 0)
                        ret.Add(snap.Documents[0].ConvertTo<UserDailyXp>());
                }
            }
        }

        return ret.OrderBy(x => x.Date).ToList();
    }

    public async Task AddDailyXp(DateTime date, ulong userId, long xp)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("XpChartHistory").Document($"{userId}").Collection("History").Document("Year").Collection($"{date.Year}").Document($"{date.DayOfYear}");

        Dictionary<string, object> data = new()
        {
            { "UserId", userId },
            { "Xp", xp },
            { "Date", date },
        };

        await SetIfAbsentAsync(doc, data);
    }

    public async Task AddOrRemoveUserXp(ulong userId, int action, int total, int booster, int challenges, int eventos, int intercambios, int otros)
    {
        FirestoreDb db = await GetDbAsync();

        DocumentReference doc = db.Collection("XpUsers").Document($"{userId}");
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
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
            UserXp registro = snap.ConvertTo<UserXp>();

            long booster1, challenges1, eventos1, intercambios1, otros1;

            long total1 = registro.Total;
            if (action == 0)
            {
                total1 += total;
                booster1 = registro.Booster; booster1 += booster;
                challenges1 = registro.Challenges; challenges1 += challenges;
                eventos1 = registro.Eventos; eventos1 += eventos;
                intercambios1 = registro.Intercambios; intercambios1 += intercambios;
                otros1 = registro.Otros; otros1 += otros;
            }
            else
            {
                total1 -= total;
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

    public async Task<List<UserXp>> GetRanking()
    {
        FirestoreDb db = await GetDbAsync();
        return await QueryListAsync<UserXp>(db.Collection("XpUsers"));
    }

    public async Task SetCooldownTeiou(ulong userId)
    {
        FirestoreDb db = await GetDbAsync();
        DocumentReference doc = db.Collection("TeiouCooldown").Document($"Nickname").Collection("Usuarios").Document($"{userId}");

        DateTime date = new DateTime(day: DateTime.UtcNow.Day, month: DateTime.UtcNow.Month, year: DateTime.UtcNow.Year, hour: DateTime.UtcNow.Hour, minute: DateTime.UtcNow.Minute, second: DateTime.UtcNow.Second, kind: DateTimeKind.Utc);
        date = date.AddHours(24);

        Dictionary<string, object> data = new()
        {
            { "UserId", userId },
            { "Cooldown", date }
        };

        await UpsertAsync(doc, data);
    }

    public async Task<List<TeiouCooldownNickname>> GetListTeiouNicknameCooldown()
    {
        FirestoreDb db = await GetDbAsync();
        CollectionReference col = db.Collection("TeiouCooldown").Document($"Nickname").Collection("Usuarios");
        return await QueryListAsync<TeiouCooldownNickname>(col);
    }
}
