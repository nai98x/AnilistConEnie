using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace AnilistESP
{
    public static class Funciones
    {
        private static readonly UsuariosAnilist usuariosAnilist = new();
        private static readonly Random rng = new();

        public static FirestoreDb GetFirestoreClientYumiko()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-yumiko.json";
            var jsonString = File.ReadAllText(path);
            var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
            return FirestoreDb.Create("yumiko-1590195019393", builder.Build());
        }

        public static async Task<FirestoreDb> GetFirestoreClientAnilistConEnie()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-anilistconenie.json";
            var jsonString = File.ReadAllText(path);
            var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
            return await FirestoreDb.CreateAsync("anilistconenie-e09cb", builder.Build());
        }

        public static DiscordChannel GetCanalUsuariosAnilist(DiscordClient client, DiscordGuild guild)
        {
            if (guild.Id == 862408834693070898) // Añilist
            {
                return guild.GetChannel(862934726553501736);
            }
            else
            {
                return null;
            }
        }

        public static async Task BorrarMensajeUsuarioAnilist(DiscordClient client, DiscordGuild guild, long oldMessageId)
        {
            DiscordChannel canal = GetCanalUsuariosAnilist(client, guild);
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

        public static DiscordEmoji ToEmoji(string text)
        {
            text = text.Trim();
            var match = Regex.Match(text, @"^<?a?:?([a-zA-Z0-9_]+):([0-9]+)>?$");
            if (!match.Success) return DiscordEmoji.TryFromUnicode(text, out var emoji) ? emoji : null;
            string json = $"{{\"name\":\"{match.Groups[1].Value}\", \"id\":{match.Groups[2].Value}," +
                $"\"animated\":{text.StartsWith("<a:").ToString().ToLower()}, \"require_colons\":true, \"available\":true}}";
            return JsonConvert.DeserializeObject<DiscordEmoji>(json);
        }

        public static int GetNumeroRandom(int min, int max)
        {
            if (min <= 0 && max <= 0)
                return 0;
            Random rnd = new();
            return rnd.Next(minValue: min, maxValue: max);
        }

        public static string NormalizarField(string s)
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

        public static string NormalizarDescription(string s)
        {
            if (s.Length > 2048)
            {
                string aux = s.Remove(2048);
                int index = aux.LastIndexOf('[');
                if (index != -1)
                    return aux.Remove(aux.LastIndexOf('[')) + "...";
                else
                    return aux.Remove(aux.Length - 4) + " ...";
            }
            return s;
        }

        public static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static EmbedFooter GetFooter(InteractionContext ctx) => new()
        {
            Text = $"Invocado por {ctx.Member.DisplayName} ({ctx.Member.Username})",
            IconUrl = ctx.Member.AvatarUrl
        };

        public static EmbedFooter GetFooter(Context ctx) => new()
        {
            Text = $"Invocado por {ctx.Member.DisplayName} ({ctx.Member.Username})",
            IconUrl = ctx.Member.AvatarUrl
        };

        public static EmbedAuthor GetAuthor(string nombre, string avatar, string url)
        {
            return new EmbedAuthor()
            {
                IconUrl = avatar,
                Name = nombre,
                Url = url
            };
        }

        public static DiscordColor GetColor()
        {
            return DiscordColor.Blurple;
        }

        public static string QuitarCaracteresEspeciales(string str)
        {
            if (str != null)
                return Regex.Replace(str, @"[^a-zA-Z0-9]+", " ").Trim();
            return null;
        }

        public async static Task BorrarMensaje(Context ctx, ulong msgId)
        {
            if (ChequearPermisoBot(ctx, Permissions.ManageMessages))
            {
                try
                {
                    var mensaje = await ctx.Channel.GetMessageAsync(msgId);
                    if (mensaje != null)
                    {
                        await mensaje.DeleteAsync("Auto borrado de Yumiko");
                    }
                }
                catch (Exception) { }
            }
        }

        public static bool ChequearPermisoBot(Context ctx, Permissions permiso)
        {
            return PermissionMethods.HasPermission(ctx.Channel.PermissionsFor(ctx.Guild.CurrentMember), permiso);
        }

        public static bool ChequearPermisoMember(Context ctx, DiscordMember member, Permissions permiso)
        {
            return PermissionMethods.HasPermission(ctx.Channel.PermissionsFor(member), permiso);
        }

        public async static Task GrabarLogError(Context ctx, string descripcion)
        {
            var Guild = await ctx.Client.GetGuildAsync(713809173573271613);
            if (Guild != null)
            {
                var ChannelErrores = Guild.GetChannel(840440877565739008);
                if (ChannelErrores != null)
                {
                    await ChannelErrores.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Title = "Error no controlado",
                        Description = descripcion,
                        Color = DiscordColor.Red,
                        Footer = GetFooter(ctx),
                        Author = new EmbedAuthor
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

        public async static Task GrabarLogUsuarioOutAnilist(DiscordClient Client, DiscordMember user, DiscordGuild guild)
        {
            var Guild = await Client.GetGuildAsync(787033852258418768);
            var ChannelErrores = Guild.GetChannel(854383940231233597);
            await ChannelErrores.SendMessageAsync(new DiscordEmbedBuilder
            {
                Title = "Perfil eliminado",
                Description = $"{user.DisplayName} ya no está en el servidor y se ha borrado su perfil de Anilist",
                Color = GetColor(),
                Author = new EmbedAuthor
                {
                    IconUrl = guild.IconUrl,
                    Name = guild.Name
                }
            });
        }

        public async static Task<bool> GetSiNoInteractivity(Context ctx, InteractivityExtension interactivity, string titulo, string descripcion)
        {
            DiscordButtonComponent buttonSi = new(ButtonStyle.Success, "true", "Si");
            DiscordButtonComponent buttonNo = new(ButtonStyle.Danger, "false", "No");

            DiscordMessageBuilder mensajeRondas = new()
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

        public static List<ulong> IDRolesColoresAnilistEsp2()
        {
            List<ulong> ret = new();

            ret.Add(862813602527707187); // Bright Navy Blue
            ret.Add(862813733381865482); // Picton Blue
            ret.Add(862813867343609896); // Baby Blue
            ret.Add(862813949430595625); // Non-Photo Blue
            ret.Add(862814035807174756); // Electric Blue
            ret.Add(862814112631226388); // Pale Aquamarine
            ret.Add(863155130123026463); // Yellow
            ret.Add(862877534165401631); // Meat Brown
            ret.Add(862877649454104598); // Naples Yellow
            ret.Add(862877720899616788); // Brown
            ret.Add(862877806656749587); // Blast-Off Bronze
            ret.Add(862877878442393600); // Rose Ebony
            ret.Add(862877947265155072); // Coral
            ret.Add(862878045371498527); // Deep Saffron
            ret.Add(862878114999435275); // Orange
            ret.Add(862878195601637376); // Giants Orange
            ret.Add(862878279626522624); // Bright red
            ret.Add(862878351965290556); // Dark Red
            ret.Add(862878454302769193); // Fire Brick
            ret.Add(862879022722449460); // Red
            ret.Add(862879102380671007); // Light Coral
            ret.Add(862879172980768819); // Indian Red
            ret.Add(862879294452006913); // Salmon
            ret.Add(862879403154604062); // Light Salmon
            ret.Add(862879505759993857); // Lavender Blush
            ret.Add(862879631744958474); // Pale Pink
            ret.Add(862879709166436372); // Cameo Pink
            ret.Add(862879806604181515); // Lavender Rose
            ret.Add(862879911328612352); // Sky Magenta
            ret.Add(862879987068960799); // Hot Pink
            ret.Add(862880081252974610); // Frostbite
            ret.Add(862880182268461106); // Barbie Pink
            ret.Add(862880267526078474); // Blue Violet
            ret.Add(862880348571303986); // Violet
            ret.Add(862880440040816701); // Plump Purple
            ret.Add(862880517346820126); // Harlequin Green
            ret.Add(862880602264305665); // Kelly Green
            ret.Add(862880684700073984); // Pastel Green
            ret.Add(862880766324768798); // Light Green
            ret.Add(862880851221413918); // Granny Smith Apple
            ret.Add(862880971249418251); // Tea Green
            ret.Add(862881612441845790); // White
            ret.Add(862881726756945930); // Black

            return ret;
        }

        public static List<ulong> IDRolesPaisesAnilistEsp2()
        {
            List<ulong> ret = new();

            ret.Add(863687575331012618); // Argentina
            ret.Add(863688124696625153); // Bolivia
            ret.Add(863687136543899658); // Chile
            ret.Add(863687047842889748); // Colombia
            ret.Add(863686892549570560); // Costa Rica
            ret.Add(863688054279766068); // Cuba
            ret.Add(863687910997229591); // Ecuador
            ret.Add(863687219448643584); // El Salvador
            ret.Add(863687790762655794); // España
            ret.Add(863687501734215692); // Guatemala
            ret.Add(863688360178876416); // Honduras
            ret.Add(863687990349135882); // México
            ret.Add(863688263122681877); // Nicaragua
            ret.Add(863688518081314846); // Panama
            ret.Add(863687727065202708); // Paraguay
            ret.Add(863688438842785812); // Peru
            ret.Add(863687661150797844); // Puerto Rico
            ret.Add(863688589572702208); // Rep. Dominicana
            ret.Add(863687410881265674); // Uruguay
            ret.Add(863687332880580609); // Venezuela

            return ret;
        }

        public static Context GetContext(InteractionContext itx)
        {
            return new()
            {
                Client = itx.Client,
                Channel = itx.Channel,
                Guild = itx.Guild,
                Member = itx.Member,
                User = itx.User,
                Interaction = itx.Interaction
            };
        }

        public static DiscordEmbedBuilder LogInteractionCommand(dynamic e, string titulo, bool parms, bool errored)
        {
            var builder = new DiscordEmbedBuilder()
            {
                Title = titulo,
                Footer = new EmbedFooter()
                {
                    Text = $"{e.Context.User.DisplayName}",
                    IconUrl = e.Context.User.AvatarUrl
                },
                Author = new EmbedAuthor()
                {
                    IconUrl = e.Context.Guild.IconUrl,
                    Name = $"{e.Context.Guild.Name}"
                }
            }.AddField("Id Servidor", $"{e.Context.Guild.Id}", true)
            .AddField("Id Canal", $"{e.Context.Channel.Id}", true)
            .AddField("Id Usuario", $"{e.Context.User.Id}", true)
            .AddField("Canal", $"#{e.Context.Channel.Name}", false);

            if (errored)
            {
                builder.WithDescription($"{e.Exception.Message}\n```{e.Exception.StackTrace}```");
                builder.WithColor(DiscordColor.Red);
            }
            else
            {
                builder.WithColor(DiscordColor.Green);
            }

            if (parms)
            {
                string options = string.Empty;
                var args = e.Context.Interaction.Data.Options;
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        options += $"`{arg.Name}: {arg.Value}` ";
                    }
                }
                builder.AddField("Comando", $"/{e.Context.CommandName} {options}", false);
            }
            else
            {
                builder.AddField("Comando", $"/{e.Context.CommandName}", false);
            }

            return builder;
        }

        public static async Task<byte[]> MergeImage(string link1, string link2, int x, int y)
        {
            var client = new HttpClient();
            var bytes1 = await client.GetByteArrayAsync(link1);
            var bytes2 = await client.GetByteArrayAsync(link2);

            using var memoryStream = new MemoryStream();
            using Image<Rgba32> img1 = Image.Load<Rgba32>(bytes1); // load up source images
            using Image<Rgba32> img2 = Image.Load<Rgba32>(bytes2);

            using var outputImage = new Image<Rgba32>(x, y); // create output image of the correct dimensions

            img1.Mutate(o => o.Resize(new Size(x / 2, y)));
            img2.Mutate(o => o.Resize(new Size(x / 2, y)));

            // take the 2 source images and draw them onto the image
            outputImage.Mutate(o => o
                .DrawImage(img1, new Point(0, 0), 1f) // draw the first one top left
                .DrawImage(img2, new Point(x / 2, 0), 1f)); // draw the second next to it

            // This saves to the memoryStream with encoder
            outputImage.Save(memoryStream, new PngEncoder());
            memoryStream.Position = 0; // The position needs to be reset.

            // return byte[]
            return memoryStream.ToArray();
        }

        public static byte[] OverlapImage(byte[] image1, byte[] image2, int x, int y)
        {
            using var memoryStream = new MemoryStream();
            using var outputImage = new Image<Rgba32>(x, y);
            using Image<Rgba32> img1 = Image.Load<Rgba32>(image1);
            using Image<Rgba32> img2 = Image.Load<Rgba32>(image2);

            outputImage.Mutate(o => o
                .DrawImage(img1, new Point(0, 0), 1f)
                .DrawImage(img2, new Point(0, 0), 1f));

            outputImage.Save(memoryStream, new PngEncoder());
            memoryStream.Position = 0;

            return memoryStream.ToArray();
        }

        public static MemoryStream ToMemoryStream(byte[] byteArray)
        {
            return new MemoryStream(byteArray)
            {
                Position = 0,
            };
        }

        public static string LimpiarTexto(string texto)
        {
            if (texto != null)
            {
                texto = texto.Replace("<br>", "");
                texto = texto.Replace("<Br>", "");
                texto = texto.Replace("<bR>", "");
                texto = texto.Replace("<BR>", "");
                texto = texto.Replace("<i>", "*");
                texto = texto.Replace("<I>", "*");
                texto = texto.Replace("</i>", "*");
                texto = texto.Replace("</I>", "*");
                texto = texto.Replace("~!", "||");
                texto = texto.Replace("!~", "||");
                texto = texto.Replace("__", "**");
                texto = texto.Replace("<b>", "**");
                texto = texto.Replace("<B>", "**");
                texto = texto.Replace("</b>", "**");
                texto = texto.Replace("</B>", "**");
            }
            else
            {
                texto = string.Empty;
            }
            return texto;
        }

        public async static Task<int> GetElegido(InteractionContext ctx, List<string> opciones)
        {
            int cantidadOpciones = opciones.Count;
            if (cantidadOpciones == 1)
                return 1;
            else
            {
                var interactivity = ctx.Client.GetInteractivity();

                List<DiscordComponent> componentes = new();
                int i = 0;
                foreach (var opc in opciones)
                {
                    if (i > 5)
                    {
                        break;
                    }
                    var aux = NormalizarBoton(opc);
                    i++;
                    DiscordButtonComponent button = new(ButtonStyle.Primary, $"{i}", $"{aux}");
                    componentes.Add(button);
                }

                var embed = new DiscordEmbedBuilder
                {
                    Color = GetColor(),
                    Title = "Elije la opcion",
                };
                DiscordMessage elegirMsg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddComponents(componentes).AddEmbed(embed));

                var msgElegirInter = await interactivity.WaitForButtonAsync(elegirMsg, ctx.User, TimeSpan.FromSeconds(Convert.ToDouble(ConfigurationManager.AppSettings["TimeoutGeneral"])));

                if (!msgElegirInter.TimedOut)
                {
                    var resultElegir = msgElegirInter.Result;
                    return int.Parse(resultElegir.Id);
                }
            }
            return -1;
        }

        public static string NormalizarBoton(string s)
        {
            if (s.Length > 80)
            {
                return s.Remove(76) + " ...";
            }
            return s;
        }

        public static async Task<string> ObtenerTokenTatsu()
        {
            var json = string.Empty;
            using (var fs = File.OpenRead("config.json"))
            {
                using var sr = new StreamReader(fs, new UTF8Encoding(false));
                json = await sr.ReadToEndAsync().ConfigureAwait(false);
            }

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            return configJson.Tatsu_token;
        }

        public static Stream CrearArchivo(AnimeLinks links)
        {
            string path = $@"c:\temp\descargaLinks.txt";
            using (FileStream fs = File.Create(path))
            {
                string linksList = $"Links de descarga para {links.Name}\n\n";
                var hosts = links.Hosts;
                foreach (var host in hosts)
                {
                    linksList += $"Servidor: {host.Name}\n";
                    var linkList = host.Links;
                    foreach (var l in linkList)
                    {
                        linksList += $"{l.Number} - {l.Href}\n";
                    }
                    linksList += "\n";
                }
                byte[] info = new UTF8Encoding(true).GetBytes(linksList);
                fs.Write(info, 0, info.Length);
            }
            return File.OpenRead(path);
        }
    }
}
