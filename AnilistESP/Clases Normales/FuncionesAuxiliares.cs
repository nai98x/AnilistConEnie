using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus;
using System.Configuration;
using Google.Cloud.Firestore;
using static DSharpPlus.Entities.DiscordEmbedBuilder;
using DSharpPlus.Interactivity;

namespace AnilistESP
{
    public class FuncionesAuxiliares
    {
        public FirestoreDb GetFirestoreClient()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
            return FirestoreDb.Create(ConfigurationManager.AppSettings["NombreDbFirebase"]);
        }

        public async Task<DiscordChannel> GetCanalUsuariosAnilist(DiscordClient client)
        {
            IDebuggingService mode = new DebuggingService();
            bool debug = mode.RunningInDebugMode();
            if (debug)
            {
                var guild = await client.GetGuildAsync(853766076122005565);
                return guild.GetChannel(854476365834485770);
            }
            else
            {
                var guild = await client.GetGuildAsync(701813281718927441);
                return guild.GetChannel(854772817667948574);
            }
        }

        public async Task BorrarMensajeUsuarioAnilist(DiscordClient client, long oldMessageId)
        {
            DiscordChannel canal = await GetCanalUsuariosAnilist(client);
            DiscordMessage mensaje = await canal.GetMessageAsync((ulong)oldMessageId);
            if (mensaje != null)
            {
                try
                {
                    await mensaje.DeleteAsync("Auto borrado de Yumiko");
                }
                catch (Exception) { }
            }
        }

        public int GetNumeroRandom(int min, int max)
        {
            if (min <= 0 && max <= 0)
                return 0;
            Random rnd = new Random();
            return rnd.Next(minValue: min, maxValue: max);
        }

        public string NormalizarField(string s)
        {
            if (s.Length > 1024)
            {
                string aux = s.Remove(1024);
                int index = aux.LastIndexOf('[');
                if (index != -1)
                    return aux.Remove(aux.LastIndexOf('[')) + "...";
                else
                    return aux.Remove(aux.Length - 4) + " ...";
            }
            return s;
        }

        public string NormalizarDescription(string s)
        {
            if (s.Length > 2048)
            {
                string aux = s.Remove(2048);
                int index = aux.LastIndexOf('[');
                if(index != -1)
                    return aux.Remove(aux.LastIndexOf('[')) + "...";
                else
                    return aux.Remove(aux.Length-4) + " ...";
            }
            return s;
        }

        public string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public EmbedFooter GetFooter(CommandContext ctx)
        {
            return  new EmbedFooter()
            {
                Text = $"Invocado por {ctx.Member.DisplayName} ({ctx.Member.Username}#{ctx.Member.Discriminator}) | {ctx.Prefix}{ctx.Command.Name}",
                IconUrl = ctx.Member.AvatarUrl
            };
        }

        public EmbedAuthor GetAuthor(string nombre, string avatar, string url)
        {
            return new EmbedAuthor()
            {
                IconUrl = avatar,
                Name = nombre,
                Url = url
            };
        }

        public DiscordColor GetColor()
        {
            return DiscordColor.Blurple;
        }

        public string QuitarCaracteresEspeciales(string str)
        {
            if(str != null)
                return Regex.Replace(str, @"[^a-zA-Z0-9]+", " ").Trim();
            return null;
        }

        public bool ChequearPermisoYumiko(CommandContext ctx, DSharpPlus.Permissions permiso)
        {
            return DSharpPlus.PermissionMethods.HasPermission(ctx.Channel.PermissionsFor(ctx.Guild.CurrentMember), permiso);
        }

        public async Task BorrarMensaje(CommandContext ctx, ulong msgId)
        {
            if(ChequearPermisoYumiko(ctx, Permissions.ManageMessages))
            {
                try
                {
                    var mensaje = await ctx.Channel.GetMessageAsync(msgId);
                    if (mensaje != null)
                    {
                        await mensaje.DeleteAsync("Auto borrado de Yumiko");
                    }
                }
                catch (Exception){ }
            }
        }

        public async Task<DateTime?> CrearDate(CommandContext ctx)
        {
            DiscordMessage msgDia, msgMes, msgAnio, error;
            DSharpPlus.Interactivity.InteractivityResult<DiscordMessage> msgDiaInter, msgMesInter, msgAnioInter;
            var interactivity = ctx.Client.GetInteractivity();
            msgDia = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
            {
                Title = "Escribe el dia tu fecha de nacimiento",
                Description = "Ejemplo: 30"
            });
            msgDiaInter = await interactivity.WaitForMessageAsync(xm => xm.Channel == ctx.Channel && xm.Author == ctx.User, TimeSpan.FromSeconds(60));
            if (!msgDiaInter.TimedOut)
            {
                bool resultDia = int.TryParse(msgDiaInter.Result.Content, out int dia);
                if (resultDia)
                {
                    msgMes = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                    {
                        Title = "Escribe el mes tu fecha de nacimiento",
                        Description = "Ejemplo: 1"
                    });
                    msgMesInter = await interactivity.WaitForMessageAsync(xm => xm.Channel == ctx.Channel && xm.Author == ctx.User, TimeSpan.FromSeconds(60));
                    if (!msgMesInter.TimedOut)
                    {
                        bool resultMes = int.TryParse(msgMesInter.Result.Content, out int mes);
                        if (resultMes)
                        {
                            msgAnio = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                            {
                                Title = "Escribe el año tu fecha de nacimiento",
                                Description = "Ejemplo: 2000"
                            });
                            msgAnioInter = await interactivity.WaitForMessageAsync(xm => xm.Channel == ctx.Channel && xm.Author == ctx.User, TimeSpan.FromSeconds(60));
                            if (!msgAnioInter.TimedOut)
                            {
                                bool resultAnio = int.TryParse(msgAnioInter.Result.Content, out int anio);
                                if (resultAnio)
                                {
                                    bool result = DateTime.TryParse($"{dia}/{mes}/{anio}", CultureInfo.CreateSpecificCulture("es-ES"), DateTimeStyles.None, out DateTime fecha);
                                    if (result)
                                    {
                                        if(fecha < DateTime.Today)
                                        {
                                            await BorrarMensaje(ctx, msgDia.Id);
                                            await BorrarMensaje(ctx, msgDiaInter.Result.Id);
                                            await BorrarMensaje(ctx, msgMes.Id);
                                            await BorrarMensaje(ctx, msgMesInter.Result.Id);
                                            await BorrarMensaje(ctx, msgAnio.Id);
                                            await BorrarMensaje(ctx, msgAnioInter.Result.Id);
                                            return fecha;
                                        }
                                        else
                                        {
                                            error = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                                            {
                                                Title = "Error",
                                                Description = "La fecha de cumpleaños no puede ser posterior a la actual",
                                                Footer = GetFooter(ctx),
                                                Color = GetColor()
                                            });
                                        }
                                    }
                                    else
                                    {
                                        error = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                                        {
                                            Title = "Error",
                                            Description = $"La fecha `{dia}/{mes}/{anio}` no es real",
                                            Footer = GetFooter(ctx),
                                            Color = GetColor()
                                        });
                                    }
                                }
                                else
                                {
                                    error = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                                    {
                                        Title = "Error",
                                        Description = "El año debe ser un numero",
                                        Footer = GetFooter(ctx),
                                        Color = GetColor()
                                    });
                                }
                            }
                            else
                            {
                                error = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                                {
                                    Title = "Error",
                                    Description = "Tiempo agotado esperando el año",
                                    Footer = GetFooter(ctx),
                                    Color = GetColor()
                                });
                            }
                            if (msgAnio != null)
                                await BorrarMensaje(ctx, msgAnio.Id);
                            if (msgAnioInter.Result != null)
                                await BorrarMensaje(ctx, msgAnioInter.Result.Id);
                        }
                        else
                        {
                            error = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                            {
                                Title = "Error",
                                Description = "El mes debe ser un numero",
                                Footer = GetFooter(ctx),
                                Color = GetColor()
                            });
                        }
                    }
                    else
                    {
                        error = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                        {
                            Title = "Error",
                            Description = "Tiempo agotado esperando el mes",
                            Footer = GetFooter(ctx),
                            Color = GetColor()
                        });
                    }
                    if (msgMes != null)
                        await BorrarMensaje(ctx, msgMes.Id);
                    if (msgMesInter.Result != null)
                        await BorrarMensaje(ctx, msgMesInter.Result.Id);
                }
                else
                {
                    error = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                    {
                        Title = "Error",
                        Description = "El dia debe ser un numero",
                        Footer = GetFooter(ctx),
                        Color = GetColor()
                    });
                }
            }
            else
            {
                error = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = "Tiempo agotado esperando el dia",
                    Footer = GetFooter(ctx),
                    Color = GetColor()
                });
            }
            if(msgDia != null)
                await BorrarMensaje(ctx, msgDia.Id);
            if(msgDiaInter.Result != null)
                await BorrarMensaje(ctx, msgDiaInter.Result.Id);
            if (error != null)
            {
                await Task.Delay(5000);
                await BorrarMensaje(ctx, error.Id);
            }
            return null;
        }

        public async Task GrabarLogError(CommandContext ctx, string descripcion)
        {
            var Guild = await ctx.Client.GetGuildAsync(713809173573271613);
            if(Guild != null)
            {
                var ChannelErrores = Guild.GetChannel(840440877565739008);
                if(ChannelErrores != null)
                {
                    await ChannelErrores.SendMessageAsync(new DiscordEmbedBuilder { 
                        Title = "Error no controlado",
                        Description = descripcion,
                        Color = DiscordColor.Red,
                        Footer = GetFooter(ctx),
                        Author= new EmbedAuthor
                        {
                            IconUrl = ctx.Guild.IconUrl,
                            Name = ctx.Guild.Name
                        },
                    }.AddField("Id Servidor", $"{ctx.Guild.Id}", true)
                    .AddField("Id Canal", $"{ctx.Channel.Id}", true)
                    .AddField("Canal", $"#{ctx.Channel.Name}", false)
                    .AddField("Mensaje", $"{ctx.Message.Content}", false));
                }
            }
        }

        public async Task GrabarLogUsuarioOutAnilist(DiscordClient Client, DiscordMember user)
        {
            ulong guildId;
            ulong channelId;
            IDebuggingService mode = new DebuggingService();
            bool Debug = mode.RunningInDebugMode();
            if (Debug)
            {
                guildId = 787033852258418768;
                channelId = 854383940231233597;
            }
            else
            {
                guildId = 701813281718927441;
                channelId = 702997924740726795;
            }
            var Guild = await Client.GetGuildAsync(guildId);
            var ChannelErrores = Guild.GetChannel(channelId);
            await ChannelErrores.SendMessageAsync(new DiscordEmbedBuilder
            {
                Title = "Perfil eliminado",
                Description = $"{user.Username}#{user.Discriminator} ya no está en el servidor y se ha borrado su perfil de Anilist",
                Color = GetColor()
            });
        }

        public async Task<string> GetStringInteractivity(CommandContext ctx, string tituloBusqueda, string descBusqueda, string descError)
        {
            var interactivity = ctx.Client.GetInteractivity();
            var msgUsuario = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
            {
                Title = tituloBusqueda,
                Description = descBusqueda,
                Footer = GetFooter(ctx),
                Color = GetColor(),
            });
            var msgUserInter = await interactivity.WaitForMessageAsync(xm => xm.Channel == ctx.Channel && xm.Author == ctx.User, TimeSpan.FromSeconds(Convert.ToDouble(ConfigurationManager.AppSettings["TimeoutGeneral"])));
            if (!msgUserInter.TimedOut)
            {
                if (msgUsuario != null)
                    await BorrarMensaje(ctx, msgUsuario.Id);
                if (msgUserInter.Result != null)
                    await BorrarMensaje(ctx, msgUserInter.Result.Id);
                return msgUserInter.Result.Content;
            }
            else
            {
                var msgError = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = descError,
                    Footer = GetFooter(ctx),
                    Color = DiscordColor.Red,
                });
                await Task.Delay(3000);
                if (msgError != null)
                    await BorrarMensaje(ctx, msgError.Id);
                if (msgUsuario != null)
                    await BorrarMensaje(ctx, msgUsuario.Id);
                return string.Empty;
            }
        }

        public async Task<bool> GetSiNoInteractivity(CommandContext ctx, InteractivityExtension interactivity, string titulo, string descripcion)
        {
            DiscordButtonComponent buttonSi = new DiscordButtonComponent(ButtonStyle.Success, "true", "Si");
            DiscordButtonComponent buttonNo = new DiscordButtonComponent(ButtonStyle.Danger, "false", "No");

            DiscordMessageBuilder mensajeRondas = new DiscordMessageBuilder()
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = titulo,
                    Description = descripcion
                }
            };

            mensajeRondas.AddComponents(buttonSi, buttonNo);

            DiscordMessage msgElegir = await mensajeRondas.SendAsync(ctx.Channel);
            var msgElegirInter = await interactivity.WaitForButtonAsync(msgElegir, ctx.User, TimeSpan.FromSeconds(Convert.ToDouble(ConfigurationManager.AppSettings["TimeoutGeneral"])));
            await BorrarMensaje(ctx, msgElegir.Id);
            if (!msgElegirInter.TimedOut)
            {
                return bool.Parse(msgElegirInter.Result.Id);
            }
            else
            {
                return false;
            }
        }
    }
}
