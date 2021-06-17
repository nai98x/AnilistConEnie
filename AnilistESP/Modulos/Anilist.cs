using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus.Entities;
using System;
using System.Configuration;
using DSharpPlus.Interactivity.Extensions;
using System.Globalization;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Text.RegularExpressions;

namespace AnilistESP
{
    public class Anilist : BaseCommandModule
    {
        private readonly FuncionesAuxiliares funciones = new FuncionesAuxiliares();
        private readonly GraphQLHttpClient graphQLClient = new GraphQLHttpClient("https://graphql.anilist.co", new NewtonsoftJsonSerializer());
        private readonly UsuariosAnilist usuariosAnilist = new UsuariosAnilist();

        [Command("setanilist"), Description("Registra tu anilist.")]
        public async Task SetAnilist(CommandContext ctx, string usuario = null)
        {
            if (String.IsNullOrEmpty(usuario))
            {
                usuario = await funciones.GetStringInteractivity(ctx, "Escriba un nombre de usuario de AniList", "Ejemplo: Josh", "Tiempo agotado esperando el usuario de AniList");
            }
            if (!String.IsNullOrEmpty(usuario))
            {
                Uri uriResult;
                bool porUrl = Uri.TryCreate(usuario, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (porUrl)
                {
                    bool match = usuario.Contains("https://anilist.co/user/");
                    if (match)
                    {
                        string inputUrl = usuario.Trim();
                        string userName = inputUrl;
                        if (inputUrl.EndsWith("/"))
                        {
                            userName = inputUrl.Remove(inputUrl.Length - 1);
                        }

                        var index = userName.LastIndexOf('/');
                        usuario = userName.Substring(index + 1);
                    }
                    else
                    {
                        var msg = await ctx.Channel.SendMessageAsync($"{ctx.User.Mention}, debes ingresar la URL de tu perfil de Anilist!\n" +
                            $"Ejemplo: https://anilist.co/user/Josh/").ConfigureAwait(false);
                        await Task.Delay(3000);
                        await funciones.BorrarMensaje(ctx, msg.Id);
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
                        nombre = usuario
                    }
                };
                try
                {
                    var data = await graphQLClient.SendQueryAsync<dynamic>(request);
                    if (data.Data != null)
                    {
                        string siteurl = data.Data.User.siteUrl;

                        DiscordChannel channel = await funciones.GetCanalUsuariosAnilist(ctx.Client);
                        DiscordMessage mensaje = await channel.SendMessageAsync($"**Perfil de {ctx.User.Mention}**\n\n{siteurl}");
                        await usuariosAnilist.SetAnilist(ctx, siteurl, mensaje.Id, ctx.User.Id);
                        var msg = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Footer = funciones.GetFooter(ctx),
                            Title = "Perfil guardado",
                            Description = $"{ctx.User.Mention}, haz guardado tu perfil de Anilist satisfactoriamente"
                        });
                        await Task.Delay(5000);
                        await funciones.BorrarMensaje(ctx, msg.Id);
                    }
                    else
                    {
                        foreach (var x in data.Errors)
                        {
                            var msg = await ctx.Channel.SendMessageAsync($"Error: {x.Message}").ConfigureAwait(false);
                            await Task.Delay(3000);
                            await funciones.BorrarMensaje(ctx, msg.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DiscordMessage msg = ex.Message switch
                    {
                        "The HTTP request failed with status code NotFound" => await ctx.Channel.SendMessageAsync($"No se ha encontrado al usuario de anilist `{usuario}`").ConfigureAwait(false),
                        _ => await ctx.Channel.SendMessageAsync($"Error inesperado").ConfigureAwait(false),
                    };
                    await Task.Delay(3000);
                    await funciones.BorrarMensaje(ctx, msg.Id);
                }
            }
        }

        [Command("anilist"), Aliases("profile"), Description("Muestra un perfil de Anilist."), RequireGuild]
        public async Task AnilistProfile(CommandContext ctx, DiscordMember usuario = null)
        {
            if(usuario == null)
            {
                usuario = ctx.Member;
            }
            var userAnilist = await usuariosAnilist.GetPerfil(ctx.Guild.Id, usuario.Id);
            if (userAnilist != null)
            {
                await ctx.Channel.SendMessageAsync($"**Perfil de {usuario.DisplayName} ({usuario.Username}#{usuario.Discriminator})**\n\n{userAnilist.AnilistURL}");
            }
            else
            {
                var msg = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder { 
                    Color = DiscordColor.Red,
                    Footer = funciones.GetFooter(ctx),
                    Title = "Perfil no encontrado",
                    Description = $"{usuario.Mention} no tiene ningún usuario de Anilist vinculado con su cuenta.\n\n" +
                                $"Para agregar su perfil, el usuario debe invocar el comando `{ctx.Prefix}setanilist`"
                });
                await Task.Delay(5000);
                await funciones.BorrarMensaje(ctx, msg.Id);
            }
        }

        [Command("usuarios"), Aliases("perfiles"), Description("Muestra los perfiles registrados de Anilist."), RequireGuild]
        public async Task AnilistProfiles(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();
            var perfiles = await usuariosAnilist.GetPerfiles((long)ctx.Guild.Id);
            string profiles = string.Empty;
            int cont = 1;
            foreach (var s in perfiles)
            {
                var usuario = await ctx.Guild.GetMemberAsync((ulong)s.UserId);
                profiles += $"{cont} - {usuario.Mention}\n";
                cont++;
            }
            var embed = new DiscordEmbedBuilder
            {
                Footer = funciones.GetFooter(ctx),
                Color = funciones.GetColor(),
                Title = "Perfiles de Anilist vinculados",
                Description = "Ingresa el numero para elegir un perfil si lo deseas"
            };
            var pages = interactivity.GeneratePagesInEmbed(profiles, DSharpPlus.Interactivity.Enums.SplitType.Line, embed);
            _ = ctx.Channel.SendPaginatedMessageAsync(ctx.User, pages).ConfigureAwait(false);
            var msgElegirTagInter = await interactivity.WaitForMessageAsync(xm => xm.Channel == ctx.Channel && xm.Author == ctx.User && Regex.IsMatch(xm.Content.Trim(), @"^\d+$"), TimeSpan.FromSeconds(180));
            if (!msgElegirTagInter.TimedOut)
            {
                await funciones.BorrarMensaje(ctx, msgElegirTagInter.Result.Id);
                bool result = int.TryParse(msgElegirTagInter.Result.Content, out int numTagElegir);
                if (result && numTagElegir > 0 && numTagElegir <= perfiles.Count)
                {
                    var elegido = perfiles[numTagElegir - 1];
                    var user = await ctx.Guild.GetMemberAsync((ulong)elegido.UserId);
                    await ctx.Channel.SendMessageAsync($"**Perfil de {user.DisplayName} ({user.Username}#{user.Discriminator})**\n\n{elegido.AnilistURL}");
                }
                else
                {
                    var msg = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Footer = funciones.GetFooter(ctx),
                        Title = "Numero incorrecto",
                        Description = $"{ctx.User.Mention}, el numero que indicaste no es correcto."
                    });
                    await Task.Delay(5000);
                    await funciones.BorrarMensaje(ctx, msg.Id);
                }
            }
        }

        [Command("deleteanilist"), Aliases("eliminaranilist", "removeanilist"), Description("Elimina tu perfil de Anilist."), RequireGuild]
        public async Task AnilistProfile(CommandContext ctx)
        {
            var userAnilist = await usuariosAnilist.GetPerfil(ctx.Guild.Id, ctx.Member.Id);
            if (userAnilist != null)
            {
                await funciones.BorrarMensajeUsuarioAnilist(ctx.Client, userAnilist.MessageId);
                await usuariosAnilist.DeleteAnilist(ctx.Guild.Id, ctx.Member.Id);
                var msg = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Footer = funciones.GetFooter(ctx),
                    Title = "Perfil eliminado",
                    Description = $"{ctx.User.Mention}, haz borrado tu perfil satisfactoriamente."
                });
                await Task.Delay(5000);
                await funciones.BorrarMensaje(ctx, msg.Id);
            }
            else
            {
                var msg = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Footer = funciones.GetFooter(ctx),
                    Title = "Perfil no encontrado",
                    Description = $"{ctx.User.Mention} no tiene ningún usuario de Anilist vinculado con su cuenta."
                });
                await Task.Delay(5000);
                await funciones.BorrarMensaje(ctx, msg.Id);
            }
        }

        [Command("setanilisto"), Description("Registra tu anilist (Owner)."), Hidden, RequireOwner]
        public async Task SetAnilistO(CommandContext ctx, DiscordMember usuario, string url)
        {
            Uri uriResult;
            bool porUrl = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
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
                    url = userName.Substring(index + 1);
                }
                else
                {
                    var msg1 = await ctx.Channel.SendMessageAsync($"{ctx.User.Mention}, debes ingresar la URL de tu perfil de Anilist!\nEjemplo").ConfigureAwait(false);
                    await Task.Delay(3000);
                    await funciones.BorrarMensaje(ctx, msg1.Id);
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
            try
            {
                var data = await graphQLClient.SendQueryAsync<dynamic>(request);
                if (data.Data != null)
                {
                    string siteurl = data.Data.User.siteUrl;

                    DiscordChannel channel = await funciones.GetCanalUsuariosAnilist(ctx.Client);
                    DiscordMessage mensaje = await channel.SendMessageAsync($"**Perfil de {usuario.Mention}**\n\n{siteurl}");
                    await usuariosAnilist.SetAnilist(ctx, siteurl, mensaje.Id, usuario.Id);
                    var msg = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Footer = funciones.GetFooter(ctx),
                        Title = "Perfil guardado",
                        Description = $"{usuario.Mention}, haz guardado tu perfil de Anilist satisfactoriamente"
                    });
                    await Task.Delay(5000);
                    await funciones.BorrarMensaje(ctx, msg.Id);
                }
                else
                {
                    foreach (var x in data.Errors)
                    {
                        var msg3 = await ctx.Channel.SendMessageAsync($"Error: {x.Message}").ConfigureAwait(false);
                        await Task.Delay(3000);
                        await funciones.BorrarMensaje(ctx, msg3.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                DiscordMessage msg2 = ex.Message switch
                {
                    "The HTTP request failed with status code NotFound" => await ctx.Channel.SendMessageAsync($"No se ha encontrado al usuario de anilist `{usuario}`").ConfigureAwait(false),
                    _ => await ctx.Channel.SendMessageAsync($"Error inesperado").ConfigureAwait(false),
                };
                await Task.Delay(3000);
                await funciones.BorrarMensaje(ctx, msg2.Id);
            }
        }
    }
}
