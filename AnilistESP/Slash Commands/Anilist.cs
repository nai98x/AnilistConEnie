using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Anilist : ApplicationCommandModule
    {
        private readonly FuncionesAuxiliares funciones = new();
        private readonly UsuariosAnilist usuariosAnilist = new();

        [SlashCommand("vincularanilist", "Registra tu AniList en el servidor")]
        public async Task SetAnilist(InteractionContext ctx, [Option("Perfil", "URL o nickname de tu perfil de AniList")] string perfil)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.DeleteResponseAsync();
            var context = funciones.GetContext(ctx);
            await funciones.SetPerfilAnilist(context, perfil);
        }

        [SlashCommand("desvincularanilist", "Desvincula tu Anilist registrado en el servidor")]
        public async Task GetAnilist(InteractionContext ctx)
        {
            var userAnilist = await usuariosAnilist.GetPerfil(ctx.Guild.Id, ctx.Member.Id);
            if (userAnilist != null)
            {
                await funciones.BorrarMensajeUsuarioAnilist(ctx.Client, ctx.Guild, userAnilist.MessageId);
                await usuariosAnilist.DeleteAnilist(ctx.Guild.Id, ctx.Member.Id);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Footer = funciones.GetFooter(ctx),
                    Title = "Perfil eliminado",
                    Description = $"Haz borrado tu perfil satisfactoriamente."
                }));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Footer = funciones.GetFooter(ctx),
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
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder {
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
                    var context = funciones.GetContext(ctx);
                    await usuariosAnilist.SetAnilist(context, siteurl, usuario);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Footer = funciones.GetFooter(ctx),
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
    }
}
