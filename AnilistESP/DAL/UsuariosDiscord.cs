using DSharpPlus.CommandsNext;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class UsuariosDiscord
    {
        private readonly FuncionesAuxiliares funciones = new FuncionesAuxiliares();

        public async Task<List<UsuarioDiscordFirebase>> GetListaUsuarios(long guildId)
        {
            var ret = new List<UsuarioDiscordFirebase>();
            FirestoreDb db = funciones.GetFirestoreClient();

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

        public async Task<List<UserCumple>> GetBirthdays(CommandContext ctx, bool month)
        {
            List<UserCumple> lista = new List<UserCumple>();
            var listaFirebase = await GetListaUsuarios((long)ctx.Guild.Id);
            listaFirebase.ForEach(x =>
            {
                DateTime fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.Now.Year);
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

        public async Task<List<UserCumple>> GetBirthdaysGuild(long guildId, bool month)
        {
            List<UserCumple> lista = new List<UserCumple>();
            var listaFirebase = await GetListaUsuarios(guildId);
            listaFirebase.ForEach(x =>
            {
                DateTime fchAux = new DateTime(day: x.Birthday.Day, month: x.Birthday.Month, year: DateTime.Now.Year);
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

        public async Task SetBirthday(CommandContext ctx, DateTime fecha, bool mostrarEdad)
        {
            FirestoreDb db = funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("Cumpleaños").Document($"{ctx.Guild.Id}").Collection("Usuarios").Document($"{ctx.User.Id}");
            var snap = await doc.GetSnapshotAsync();
            UsuarioDiscordFirebase registro;
            var timeutc = new DateTime(day: fecha.Day, month: fecha.Month, year: fecha.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);
            if (snap.Exists)
            {
                registro = snap.ConvertTo<UsuarioDiscordFirebase>();
                registro.Birthday = timeutc;
                registro.MostrarYear = mostrarEdad;
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"user_id", registro.user_id},
                    {"Birthday", registro.Birthday},
                    {"MostrarYear", registro.MostrarYear},
                };
                await doc.UpdateAsync(data);
            }
            else
            {
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"user_id", ctx.User.Id},
                    {"Birthday", timeutc},
                    {"MostrarYear", mostrarEdad},
                };
                await doc.SetAsync(data);
            }
        }

        public async Task DeleteBirthday(CommandContext ctx)
        {
            FirestoreDb db = funciones.GetFirestoreClient();
            DocumentReference doc = db.Collection("Cumpleaños").Document($"{ctx.Guild.Id}").Collection("Usuarios").Document($"{ctx.User.Id}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                await doc.DeleteAsync();
            }
        }
    }
}