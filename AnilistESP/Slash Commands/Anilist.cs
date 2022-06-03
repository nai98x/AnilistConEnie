using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using GraphQL;
using GraphQL.Client.Abstractions.Utilities;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Anilist : ApplicationCommandModule
    {
        private readonly UsuariosAnilist usuariosAnilist = new();

        [SlashCommand("vincularanilist", "Registra tu AniList en el servidor")]
        public async Task SetAnilist(InteractionContext ctx, [Option("Perfil", "URL o nickname de tu perfil de AniList")] string perfil)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.DeleteResponseAsync();
            var context = Funciones.GetContext(ctx);
            await Funciones.SetPerfilAnilist(context, perfil);
        }

        [SlashCommand("desvincularanilist", "Desvincula tu Anilist registrado en el servidor")]
        public async Task GetAnilist(InteractionContext ctx)
        {
            var userAnilist = await usuariosAnilist.GetPerfil(ctx.Guild.Id, ctx.Member.Id);
            if (userAnilist != null)
            {
                await Funciones.BorrarMensajeUsuarioAnilist(ctx.Client, ctx.Guild, userAnilist.MessageId);
                await usuariosAnilist.DeleteAnilist(ctx.Guild.Id, ctx.Member.Id);
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
                    await usuariosAnilist.SetAnilist(context, siteurl, usuario);
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
                        var interactivity = ctx.Client.GetInteractivity();
                        var pages = interactivity.GeneratePagesInEmbed(embedStats, SplitType.Line, builder);

                        await ctx.DeleteResponseAsync();
                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, PaginationBehaviour.Ignore, ButtonPaginationBehavior.Disable, token: new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);
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
    }
}
