using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using GraphQL;
using GraphQL.Client.Abstractions.Utilities;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnilistConEnie.Commands
{
    public class Anilist : ApplicationCommandModule
    {
        private readonly UsuariosAnilist usuariosAnilist = new();

        [SlashCommand("desvincularanilist", "Desvincula tu Anilist registrado en el servidor")]
        public async Task GetAnilist(InteractionContext ctx)
        {
            var userAnilist = await usuariosAnilist.GetPerfil(ctx.Member.Id);
            if (userAnilist != null)
            {
                await Funciones.BorrarMensajeUsuarioAnilist(ctx.Client, ctx.Guild, userAnilist.MessageId);
                await usuariosAnilist.DeleteAnilist(ctx.Member.Id);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Footer = Funciones.GetFooter(ctx),
                    Title = "Perfil eliminado",
                    Description = $"Haz borrado tu perfil satisfactoriamente."
                }));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Footer = Funciones.GetFooter(ctx),
                    Title = "Perfil no encontrado",
                    Description = $"{ctx.User.Mention} no tiene ningún usuario de Anilist vinculado con su cuenta."
                }));
            }
        }

        [SlashCommand("modvincularanilist", "Registra un AniList en el servidor (Staff)")]
        public async Task SetAnilistMod(InteractionContext ctx, [Option("Usuario", "Usuario de Discord para asignarle el AniList")] DiscordUser user, [Option("Perfil", "URL del perfil de AniList")] string url)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            GraphQLHttpClient graphQLClient = new("https://graphql.anilist.co", new NewtonsoftJsonSerializer());
            bool porUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (porUrl)
            {
                bool match = url.Contains("https://anilist.co/user/");
                if (match)
                {
                    string inputUrl = url.Trim();
                    string userName = inputUrl;
                    if (inputUrl.EndsWith("/"))
                    {
                        userName = inputUrl.Remove(inputUrl.Length - 1);
                    }

                    var index = userName.LastIndexOf('/');
                    url = userName[(index + 1)..];
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "Error",
                        Description = "Debes ingresar la URL de tu perfil de Anilist"
                    }));
                    return;
                }
            }
            var request = new GraphQLRequest
            {
                Query =
                "query($nombre : String){" +
                "   User(search: $nombre){" +
                "       siteUrl," +
                "   }" +
                "}",
                Variables = new
                {
                    nombre = url
                }
            };
            var usuario = (DiscordMember)user;
            try
            {
                var data = await graphQLClient.SendQueryAsync<dynamic>(request);
                if (data.Data != null)
                {
                    string siteurl = data.Data.User.siteUrl;
                    var context = Funciones.GetContext(ctx);
                    await usuariosAnilist.SetAnilist(ctx.Client, ctx.Guild, siteurl, usuario);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Footer = Funciones.GetFooter(ctx),
                        Title = "Perfil guardado",
                        Description = $"{usuario.Mention}, haz guardado tu perfil de Anilist satisfactoriamente"
                    }));
                }
                else
                {
                    foreach (var x in data.Errors)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = $"{x.Message}"
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message switch
                {
                    "The HTTP request failed with status code NotFound" => $"No se ha encontrado al usuario de anilist `{usuario}`",
                    _ => $"Error inesperado: {ex.Message}",
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = $"{error}"
                }));
            }
        }

        [SlashCommand("statsserver", "Estadisticas de los usuarios del servidor de un anime o manga en AniList")]
        public async Task StatsServerAnime(
            InteractionContext ctx,
            [Option("Nombre", "Nombre del anime o manga a buscar")] string mediaNombre,
            [Choice("Anime", "anime")]
            [Choice("Manga", "manga")]
            [Option("Tipo", "Elige si buscas anime o manga")] string tipo)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var context = Funciones.GetContext(ctx);

            var media = await FuncionesAnilist.GetAniListMedia(ctx, mediaNombre, tipo);
            if (media.Ok == true)
            {
                var id = media.Id;

                if ((!media.IsAdult) || (media.IsAdult && ctx.Channel.IsNSFW))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Title = $"Buscando scores de {media.TituloRomaji}..",
                        Description = "Esto puede demorar unos minutos",
                        Color = Funciones.GetColor(),
                    }));

                    var embedStats = await FuncionesAnilist.GetScoreMediaUsuarios(ctx, id);

                    string coverImage = media.CoverImage;
                    string bannerImage = media.BannerImage;
                    string titleRomaji = media.TituloRomaji;
                    string url = media.UrlAnilist;

                    var builder = new DiscordEmbedBuilder
                    {
                        Title = $"Scores de {titleRomaji} en {ctx.Guild.Name}",
                        Color = Funciones.GetColor(),
                        ImageUrl = bannerImage,
                        Url = url,
                    }.WithThumbnail(coverImage);

                    if (media.Formato != null && media.Formato.Length > 0)
                    {
                        builder.AddField($"{DiscordEmoji.FromName(ctx.Client, ":dividers:")} Formato", Funciones.NormalizarField(media.Formato), true);
                    }

                    if (media.Estado != null && media.Estado.Length > 0)
                    {
                        builder.AddField($"{DiscordEmoji.FromName(ctx.Client, ":hourglass_flowing_sand:")} Estado", Funciones.NormalizarField(media.Estado.ToLower().ToUpperFirst()), true);
                    }

                    if (media.Fechas != null && media.Fechas.Length > 0)
                    {
                        builder.AddField($"{DiscordEmoji.FromName(ctx.Client, ":calendar_spiral:")} Fecha ", Funciones.NormalizarField(media.Fechas), false);
                    }

                    if (!string.IsNullOrEmpty(embedStats))
                    {
                        if (embedStats.Length < 4096)
                        {
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.WithDescription(embedStats)));
                        }
                        else
                        {
                            var interactivity = ctx.Client.GetInteractivity();
                            var pages = interactivity.GeneratePagesInEmbed(embedStats, SplitType.Line, builder);

                            await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, pages, asEditResponse: true, deletion: ButtonPaginationBehavior.Disable);
                        }
                    }
                    else
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = $"No se encontraron scores para {media.TituloRomaji}",
                            Color = DiscordColor.Red,
                        }));
                    }
                }
                else
                {
                    var msg = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Requiere NSFW",
                        Description = "Este comando debe ser invocado en un canal NSFW.",
                        Color = new DiscordColor(0xFF0000),
                    }));
                }
            }
            else
            {
                var msg = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(media.MsgError));
            }
        }

        //[SlashCommand("descargar", "Descarga los capitulos de un anime")]
        public async Task DescargarAnime(InteractionContext ctx, [Option("Nombre", "Nombre del anime o manga a buscar")] string buscar)
        {
            await ctx.DeferAsync();
            var context = Funciones.GetContext(ctx);
            MonoschinosDownloader animeflv = new();
            var interactivity = ctx.Client.GetInteractivity();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Buscando animes...",
                Color = Funciones.GetColor()
            }));
            var resBusqueda = await animeflv.Search(buscar);
            if (resBusqueda.Count > 0)
            {
                string resultados = string.Empty;
                int cont = 1;
                foreach (var res in resBusqueda)
                {
                    resultados += $"{cont} - **{res.Name}** ({res.Type})\n";
                    cont++;
                }
                var elegirRes = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Elije con un número el anime deseado",
                    Description = resultados,
                    Color = Funciones.GetColor()
                });
                var msgElegirInter = await interactivity.WaitForMessageAsync(xm => xm.Channel == ctx.Channel && xm.Author == ctx.User, TimeSpan.FromSeconds(Convert.ToDouble(ConfigurationManager.AppSettings["TimeoutGeneral"])));
                if (!msgElegirInter.TimedOut)
                {
                    bool result = int.TryParse(msgElegirInter.Result.Content, out int numElegir);
                    if (result)
                    {
                        if (numElegir > 0 && (numElegir <= resBusqueda.Count))
                        {
                            await Funciones.BorrarMensaje(context, elegirRes.Id);
                            await Funciones.BorrarMensaje(context, msgElegirInter.Result.Id);
                            var elegido = resBusqueda[numElegir - 1];

                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                            {
                                Title = "Descargar anime",
                                Description = $"Procesando links para **{elegido.Name}**",
                                Color = Funciones.GetColor()
                            }));

                            var links = await animeflv.GetLinks(elegido.Href, elegido.Name);
                            Dictionary<string, Stream> dic = new()
                                {
                                {"descargaLinks.txt",  (FileStream)Funciones.CrearArchivo(links)}
                            };
                            await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder
                            {
                                Content = $"{ctx.User.Mention}, aquí tienes los links para descargar **{elegido.Name}**",
                            }.AddFiles(dic));
                        }
                        else
                        {
                            var msg = await ctx.Channel.SendMessageAsync($"El número indicado debe ser valido");
                            await Task.Delay(5000);
                            await Funciones.BorrarMensaje(context, msg.Id);
                            await Funciones.BorrarMensaje(context, elegirRes.Id);
                            await Funciones.BorrarMensaje(context, msgElegirInter.Result.Id);
                        }
                    }
                    else
                    {
                        var msg = await ctx.Channel.SendMessageAsync($"La eleccion debe ser indicada con un numero");
                        await Task.Delay(5000);
                        await Funciones.BorrarMensaje(context, msg.Id);
                        await Funciones.BorrarMensaje(context, elegirRes.Id);
                        await Funciones.BorrarMensaje(context, msgElegirInter.Result.Id);
                    }
                }
                else
                {
                    var msg = await ctx.Channel.SendMessageAsync($"Tiempo agotado esperando eleccion de anime");
                    await Task.Delay(5000);
                    await Funciones.BorrarMensaje(context, msg.Id);
                    await Funciones.BorrarMensaje(context, elegirRes.Id);
                    await Funciones.BorrarMensaje(context, msgElegirInter.Result.Id);
                }
            }
        }
    }
}
