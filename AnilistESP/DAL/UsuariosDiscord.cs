using DSharpPlus.SlashCommands;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class UsuariosDiscord
    {
        public async Task<List<UsuarioDiscordFirebase>> GetListaUsuarios(long guildId)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            var ret = new List<UsuarioDiscordFirebase>();

            CollectionReference col = db.Collection("Cumpleaños").Document($"{guildId}").Collection("Usuarios");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<UsuarioDiscordFirebase>());
                }
            }

            return ret;
        }

        public async Task<List<UserCumple>> GetBirthdaysHoy(long guildId)
        {
            List<UserCumple> lista = new();
            var listaFirebase = await GetListaUsuarios(guildId);
            listaFirebase.ForEach(x =>
            {
                var fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.Now.Year);
                if (fchAux >= DateTime.Today && fchAux <= DateTime.Now)
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

        public async Task<List<UserCumple>> GetBirthdays(long guildId, bool month)
        {
            List<UserCumple> lista = new();
            var listaFirebase = await GetListaUsuarios(guildId);
            listaFirebase.ForEach(x =>
            {
                var fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.Now.Year);
                DateTime nuevoCumple;
                if (DateTime.Now > new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.Now.Year))
                    nuevoCumple = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.Now.Year + 1);
                else
                    nuevoCumple = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.Now.Year);
                if (month)
                {
                    if (fchAux >= DateTime.Now && fchAux <= DateTime.Now.AddMonths(1))
                    {
                        lista.Add(new UserCumple
                        {
                            Id = x.user_id,
                            Birthday = x.Birthday,
                            BirthdayActual = nuevoCumple,
                            MostrarYear = x.MostrarYear
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
                        MostrarYear = x.MostrarYear
                    });
                }
            });
            lista.Sort((x, y) => x.BirthdayActual.CompareTo(y.BirthdayActual));
            return lista;
        }

        public async Task SetBirthday(ulong guildId, ulong userId, DateTime fecha, bool mostrarEdad)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Cumpleaños").Document($"{guildId}").Collection("Usuarios").Document($"{userId}");
            var snap = await doc.GetSnapshotAsync();
            UsuarioDiscordFirebase registro;
            var timeutc = new DateTime(day: fecha.Day, month: fecha.Month, year: fecha.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);
            if (snap.Exists)
            {
                registro = snap.ConvertTo<UsuarioDiscordFirebase>();
                registro.Birthday = timeutc;
                registro.MostrarYear = mostrarEdad;
                Dictionary<string, object> data = new()
                {
                    { "user_id", registro.user_id },
                    { "Birthday", registro.Birthday },
                    { "MostrarYear", registro.MostrarYear },
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
                };
                await doc.SetAsync(data);
            }
        }

        public async Task DeleteBirthday(InteractionContext ctx)
        {
            FirestoreDb db = await Funciones.GetFirestoreClientAnilistConEnie();
            DocumentReference doc = db.Collection("Cumpleaños").Document($"{ctx.Guild.Id}").Collection("Usuarios").Document($"{ctx.User.Id}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                await doc.DeleteAsync();
            }
        }
    }
}