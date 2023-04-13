using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace AnilistESP
{
    public static class FuncionesAnilist
    {
        private static readonly GraphQLHttpClient _graphQlClient = new("https://graphql.anilist.co", new NewtonsoftJsonSerializer());

        public async static Task<Media> GetAniListMedia(InteractionContext ctx, string busqueda, string tipo)
        {
            string query = "query($busqueda : String){" +
            "   Page(perPage:5){" +
            "       media(type: " + tipo.ToUpper() + ", search: $busqueda){" +
            "           id," +
            "           title{" +
            "               romaji" +
            "           }," +
            "           coverImage{" +
            "               large" +
            "           }," +
            "           bannerImage," +
            "           siteUrl," +
            "           description," +
            "           format," +
            "           chapters" +
            "           episodes" +
            "           status," +
            "           meanScore," +
            "           genres," +
            "           startDate{" +
            "               year," +
            "               month," +
            "               day" +
            "           }," +
            "           endDate{" +
            "               year," +
            "               month," +
            "               day" +
            "           }," +
            "           genres," +
            "           tags{" +
            "               name," +
            "               isMediaSpoiler" +
            "           }," +
            "           synonyms," +
            "           studios{" +
            "               nodes{" +
            "                   name," +
            "                   siteUrl" +
            "               }" +
            "           }," +
            "           externalLinks{" +
            "               site," +
            "               url" +
            "           }," +
            "           isAdult" +
            "       }" +
            "   }" +
            "}";

            var request = new GraphQLRequest
            {
                Query = query,
                Variables = new
                {
                    busqueda
                }
            };

            try
            {
                var data = await _graphQlClient.SendQueryAsync<dynamic>(request);
                if (data.Data != null)
                {
                    if (data.Data.Page.media != null && data.Data.Page.media.Count > 0)
                    {
                        int cont = 0;
                        List<string> opc = new();
                        foreach (var animeP in data.Data.Page.media)
                        {
                            cont++;
                            string opcStr = animeP.title.romaji;
                            opc.Add(opcStr);
                        }
                        var elegido = await Funciones.GetElegido(ctx, opc);
                        if (elegido > 0)
                        {
                            var datos = data.Data.Page.media[elegido - 1];
                            return DecodeMedia(datos);
                        }
                        else
                        {
                            return new()
                            {
                                Ok = false,
                                MsgError = $"Tiempo agotado esperando la opción"
                            };
                        }
                    }
                }
                return new()
                {
                    Ok = false,
                    MsgError = $"No se encontró el {tipo} `{busqueda}`"
                };
            }
            catch (Exception e)
            {
                var context = Funciones.GetContext(ctx);
                await Funciones.GrabarLogError(context, $"Error en query en FuncionesAnilist - GetAnilistMedia, utilizado: {tipo}\nError: {e.Message}");
                return new()
                {
                    Ok = false,
                    MsgError = $"{e.Message}"
                };
            }
        }

        public async static Task<Media> GetAniListCharacter(InteractionContext ctx, string busqueda, string tipo)
        {
            var request = new GraphQLRequest
            {
                Query =
                "query($nombre : String){" +
                "   Page(perPage:5){" +
                "       characters(search: $nombre){" +
                "           name{" +
                "               full" +
                "           }," +
                "           image{" +
                "               large" +
                "           }," +
                "           siteUrl," +
                "           description," +
                "           animes: media(type: ANIME){" +
                "               nodes{" +
                "                   title{" +
                "                       romaji" +
                "                   }," +
                "                   siteUrl" +
                "               }" +
                "           }" +
                "           mangas: media(type: MANGA){" +
                "               nodes{" +
                "                   title{" +
                "                       romaji" +
                "                   }," +
                "                   siteUrl" +
                "               }" +
                "           }" +
                "       }" +
                "   }" +
                "}",
                Variables = new
                {
                    nombre = busqueda
                }
            };

            try
            {
                var data = await _graphQlClient.SendQueryAsync<dynamic>(request);
                if (data.Data != null && data.Data.Page.media != null)
                {
                    int cont = 0;
                    List<string> opc = new();
                    foreach (var animeP in data.Data.Page.media)
                    {
                        cont++;
                        string opcStr = animeP.title.romaji;
                        opc.Add(opcStr);
                    }
                    var elegido = await Funciones.GetElegido(ctx, opc);
                    if (elegido > 0)
                    {
                        var datos = data.Data.Page.characters[elegido - 1];
                        return DecodeCharacter(datos);
                    }
                    else
                    {
                        return new()
                        {
                            Ok = false,
                            MsgError = $"Tiempo agotado esperando la opción"
                        };
                    }
                }
                else
                {
                    return new()
                    {
                        Ok = false,
                        MsgError = $"No se encontró el {tipo} `{busqueda}`"
                    };
                }
            }
            catch (Exception e)
            {
                var context = Funciones.GetContext(ctx);
                await Funciones.GrabarLogError(context, $"Error en query en FuncionesAnilist - GetAnilistMedia, utilizado: {tipo}\nError: {e.Message}");
                return new()
                {
                    Ok = false,
                    MsgError = $"{e.Message}"
                };
            }
        }

        public static async Task<string> GetScoreMediaUsuarios(InteractionContext ctx, int mediaId)
        {
            var context = Funciones.GetContext(ctx);
            ServiciosSingleton servicio = ServiciosSingleton.GetServiciosSingleton();
            List<string> scoresList = new();

            var usuarios = servicio.Usuarios;
            var usuariosServidor = new List<UsuarioAnilistFirebase>();
            var members = await ctx.Guild.GetAllMembersAsync();

            foreach (var item in usuarios)
            {
                if (members.Any(x => x.Id == (ulong)item.UserId))
                {
                    var miembro = members.First(x => x.Id == (ulong)item.UserId);

                    DiscordRole tama = ctx.Guild.GetRole(1052997018622099548);
                    DiscordRole casual = ctx.Guild.GetRole(863525487602958336);
                    DiscordRole kouhai = ctx.Guild.GetRole(865300278491217970);
                    DiscordRole senpai = ctx.Guild.GetRole(863525246404263976);
                    DiscordRole hikikomori = ctx.Guild.GetRole(863525128403025961);
                    DiscordRole sensei = ctx.Guild.GetRole(863524938954571816);
                    DiscordRole ousama = ctx.Guild.GetRole(966815478507012106);
                    DiscordRole teiou = ctx.Guild.GetRole(966815813078224907);

                    if (miembro.Roles.Contains(tama) || miembro.Roles.Contains(casual) || miembro.Roles.Contains(kouhai) ||
                        miembro.Roles.Contains(senpai) || miembro.Roles.Contains(hikikomori) || miembro.Roles.Contains(sensei) ||
                        miembro.Roles.Contains(ousama) || miembro.Roles.Contains(teiou))
                    {
                        usuariosServidor.Add(item);
                    }
                }
            }

            var values = new List<long>();
            foreach (var user in usuariosServidor)
            {
                values.Add(long.Parse(user.AnilistURL[(user.AnilistURL.LastIndexOf("/") + 1)..]));
            }

            var lists = values.Chunk(15).ToList();

            string scores = string.Empty;
            decimal promedio = 0;
            int registros = 0;
            decimal sumaScores = 0;

            foreach (var userList in lists)
            {
                var requestPers = new GraphQLRequest
                {
                    Query =
                    @"query ($codigoMedia: Int, $ids: [Int]) {
                        Media(id: $codigoMedia) {
                            title {
                                romaji,
                                english
                            },
                            siteUrl,
                            coverImage {
                                large
                            },
                            bannerImage,
                            episodes,
                            chapters,
                            isAdult
                        },
                        Page {
                            mediaList(mediaId: $codigoMedia, userId_in: $ids) {
                                user {
                                    name,
                                    siteUrl,
                                    mediaListOptions {
                                        scoreFormat
                                    }
                                },
                                score,
                                status,
                                progress
                            }
                        }
                    }",
                    Variables = new
                    {
                        codigoMedia = mediaId,
                        ids = userList,
                    },
                };
                try
                {
                    var data = await _graphQlClient.SendQueryAsync<dynamic>(requestPers);
                    if (data.Data != null)
                    {
                        dynamic datosMedia = data.Data.Media;
                        string titleRomaji = datosMedia.title.romaji;
                        string titleEnglish = datosMedia.title.english;
                        string siteUrl = datosMedia.siteUrl;
                        string coverImage = datosMedia.coverImage.large;
                        string bannerImage = datosMedia.bannerImage;
                        string isAdult = datosMedia.isAdult;
                        string episodes = datosMedia.episodes;
                        string chapters = datosMedia.chapters;
                        string eps = string.IsNullOrEmpty(episodes) ? chapters : episodes;

                        dynamic datosMediaList = data.Data.Page.mediaList;

                        foreach (var entry in datosMediaList)
                        {
                            string name = entry.user.name;
                            string score = entry.score;
                            string url = entry.user.siteUrl;
                            string status = entry.status;
                            string progress = entry.progress;
                            string scoreFormat = entry.user.mediaListOptions.scoreFormat;
                            string scoreF = FormatearScoreUJser(scoreFormat, score);
                            decimal score100 = FormatearScoreUJser100(scoreFormat, score);

                            string pro = string.IsNullOrEmpty(eps) ? progress : progress + $"/{eps}";

                            if (!string.IsNullOrEmpty(score) && score != "0")
                            {
                                if (status == "COMPLETED")
                                {
                                    scoresList.Add($"{Formatter.MaskedUrl(name, new Uri(url))} - {scoreF}\n");
                                }
                                else
                                {
                                    scoresList.Add($"{Formatter.MaskedUrl(name, new Uri(url))} - {scoreF} {Formatter.InlineCode($"{Funciones.UppercaseFirst(status)} - Progress: {pro}")}\n");
                                }

                                registros++;
                                sumaScores += score100;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message != "The HTTP request failed with status code NotFound")
                    {
                        await Funciones.GrabarLogError(context, $"Error en GetScoreMediaUsuarios /anime: {ex.Message}\n```{ex.StackTrace}```");
                    }
                }
            }

            if (registros > 0)
            {
                promedio = sumaScores / registros;
                scores = $"{Formatter.Bold($"Promedio:")} {decimal.Round(promedio, 2)}/100\n\n";
                scoresList.Sort();
                scores += string.Join(string.Empty, values: scoresList);
            }

            return scores;
        }

        public static Media DecodeMedia(dynamic datos)
        {
            if (datos != null)
            {
                string idStr = datos.id;
                string isadult = datos.isAdult;

                Media media = new();

                media.Ok = true;
                media.Id = int.Parse(idStr);
                media.IsAdult = bool.Parse(isadult);
                media.Descripcion = datos.description;
                media.Descripcion = Funciones.NormalizarDescription(Funciones.LimpiarTexto(media.Descripcion));
                if (media.Descripcion == "")
                    media.Descripcion = "(Sin descripción)";
                media.Estado = datos.status;
                media.Episodios = datos.episodes;
                media.Chapters = datos.chapters;
                media.Formato = datos.format;
                media.Score = $"{datos.meanScore}/100";
                media.Generos = string.Empty;
                foreach (var genero in datos.genres)
                {
                    media.Generos += genero;
                    media.Generos += ", ";
                }
                if (media.Generos.Length >= 2)
                    media.Generos = media.Generos.Remove(media.Generos.Length - 2);
                media.Tags = string.Empty;
                foreach (var tag in datos.tags)
                {
                    if (tag.isMediaSpoiler == "false")
                    {
                        media.Tags += tag.name;
                    }
                    else
                    {
                        media.Tags += $"||{tag.name}||";
                    }
                    media.Tags += ", ";
                }
                if (media.Tags.Length >= 2)
                    media.Tags = media.Tags.Remove(media.Tags.Length - 2);

                media.Titulos = new();
                foreach (string title in datos.synonyms)
                {
                    media.Titulos.Add(title);
                }

                media.Estudios = string.Empty;
                var nodos = datos.studios.nodes;
                if (nodos.HasValues)
                {
                    foreach (var studio in datos.studios.nodes)
                    {
                        media.Estudios += $"[{studio.name}]({studio.siteUrl}), ";
                    }
                }
                if (media.Estudios.Length >= 2)
                    media.Estudios = media.Estudios.Remove(media.Estudios.Length - 2);
                media.LinksExternos = string.Empty;
                foreach (var external in datos.externalLinks)
                {
                    media.LinksExternos += $"[{external.site}]({external.url}), ";
                }
                if (media.LinksExternos.Length >= 2)
                    media.LinksExternos = media.LinksExternos.Remove(media.LinksExternos.Length - 2);
                if (datos.startDate.day != null)
                {
                    if (datos.endDate.day != null)
                        media.Fechas = $"{datos.startDate.day}/{datos.startDate.month}/{datos.startDate.year} al {datos.endDate.day}/{datos.endDate.month}/{datos.endDate.year}";
                    else
                        media.Fechas = $"En emisión desde {datos.startDate.day}/{datos.startDate.month}/{datos.startDate.year}";
                }
                else
                {
                    media.Fechas = $"Este anime no tiene fecha de emisión";
                }
                media.TituloRomaji = datos.title.romaji;
                media.UrlAnilist = datos.siteUrl;
                media.CoverImage = datos.coverImage.large;
                media.BannerImage = datos.bannerImage;

                return media;
            }
            else
            {
                return null;
            }
        }

        public static Character DecodeCharacter(dynamic datos)
        {
            if (datos != null)
            {
                Character character = new();

                string descripcion = datos.description;
                character.Description = Funciones.NormalizarDescription(Funciones.LimpiarTexto(descripcion));
                if (character.Description == "")
                    character.Description = "(Sin descripción)";
                character.NameFull = datos.name.full;
                character.Image = datos.image.large;
                character.SiteUrl = datos.siteUrl;
                character.Animes = new();
                foreach (var anime in datos.animes.nodes)
                {
                    character.Animes.Add(new()
                    {
                        //TituloRomaji = anime.title.romaji,
                        //UrlAnilist = anime.siteUrl
                    });
                }
                string mangas = string.Empty;
                foreach (var manga in datos.mangas.nodes)
                {
                    character.Mangas.Add(new()
                    {
                        //TitleRomaji = anime.title.romaji,
                        //SiteUrl = anime.siteUrl
                    });
                    mangas += $"[{manga.title.romaji}]({manga.siteUrl})\n";
                }

                return character;
            }
            else
            {
                return null;
            }
        }

        public async static Task<DiscordEmbedBuilder> GetInfoMediaUser(InteractionContext ctx, int anilistId, int mediaId)
        {
            var context = Funciones.GetContext(ctx);
            var requestPers = new GraphQLRequest
            {
                Query =
                    @"query ($codigoal: Int, $codigome: Int) {
                        MediaList(userId: $codigoal, mediaId: $codigome){
                            status,
                            progress,
                            startedAt {
                                year,
                                month,
                                day
                            },
                            completedAt {
                                year,
                                month,
                                day
                            },
                            notes,
                            score,
                            repeat,
                            media {
                                episodes,
                                chapters
                            },
                            user {
                                name,
                                avatar {
                                    large
                                },
                                mediaListOptions {
                                    scoreFormat
                                }
                            }
                        }
                    }",
                Variables = new
                {
                    codigoal = anilistId,
                    codigome = mediaId
                }
            };
            try
            {
                var data = await _graphQlClient.SendQueryAsync<dynamic>(requestPers);
                if (data.Data != null)
                {
                    dynamic datos = data.Data.MediaList;
                    string status = datos.status;
                    string progress = datos.progress;
                    string episodiosMedia = datos.media.episodes;
                    string chaptersMedia = datos.media.chapters;
                    string scorePers = datos.score;
                    string startedd = datos.startedAt.day;
                    string startedm = datos.startedAt.month;
                    string startedy = datos.startedAt.year;
                    string completedd = datos.completedAt.day;
                    string completedm = datos.completedAt.month;
                    string completedy = datos.completedAt.year;
                    string notas = datos.notes;
                    string rewatches = datos.repeat;
                    string scoreFormat = datos.user.mediaListOptions.scoreFormat;
                    string nameAl = datos.user.name;
                    string avatarAl = datos.user.avatar.large;

                    if (string.IsNullOrEmpty(notas))
                    {
                        notas = "(Sin notas)";
                    }

                    var builderPers = new DiscordEmbedBuilder
                    {
                        Title = $"Estadisticas de {nameAl}",
                        Description = Funciones.NormalizarDescription("**Notas**\n" + notas),
                        Color = Funciones.GetColor()
                    }.WithThumbnail(avatarAl);

                    builderPers.AddField("Estado", status, true);
                    if (!string.IsNullOrEmpty(progress))
                    {
                        string episodios = progress;
                        if (!string.IsNullOrEmpty(episodiosMedia))
                        {
                            episodios += $"/{episodiosMedia}";
                        }
                        if (!string.IsNullOrEmpty(chaptersMedia))
                        {
                            episodios += $"/{chaptersMedia}";
                        }
                        builderPers.AddField("Episodios", episodios, true);
                    }
                    string scoreMostrar = string.Empty;
                    if (!string.IsNullOrEmpty(scorePers) && !string.IsNullOrEmpty(scoreFormat) && scorePers != "0")
                    {
                        string scoreF = string.Empty;
                        switch (scoreFormat)
                        {
                            case "POINT_10":
                            case "POINT_10_DECIMAL":
                                scoreF = $"{scorePers}/10";
                                break;
                            case "POINT_100":
                                scoreF = $"{scorePers}/100";
                                break;
                            case "POINT_5":
                                int scoreS = int.Parse(scorePers);
                                for (int i = 0; i < scoreS; i++)
                                {
                                    scoreF += "★";
                                }
                                break;
                            case "POINT_3":
                                int score3 = int.Parse(scorePers);
                                switch (score3)
                                {
                                    case 1:
                                        scoreF = "🙁";
                                        break;
                                    case 2:
                                        scoreF = "😐";
                                        break;
                                    case 3:
                                        scoreF = "🙂";
                                        break;
                                }
                                break;
                        }
                        builderPers.AddField("Puntuación", scoreF, true);
                    }
                    else
                    {
                        builderPers.AddField("Puntuación", "No asignada", true);
                    }
                    if (!string.IsNullOrEmpty(rewatches))
                    {
                        builderPers.AddField("Rewatches", $"{rewatches}", false);
                    }
                    if (!string.IsNullOrEmpty(startedd) && !string.IsNullOrEmpty(startedm) && !string.IsNullOrEmpty(startedy))
                    {
                        builderPers.AddField("Fecha de inicio", $"{startedd}/{startedm}/{startedy}", true);
                    }
                    if (!string.IsNullOrEmpty(completedd) && !string.IsNullOrEmpty(completedm) && !string.IsNullOrEmpty(completedy))
                    {
                        builderPers.AddField("Fecha completado", $"{completedd}/{completedm}/{completedy}", true);
                    }

                    return builderPers;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "The HTTP request failed with status code NotFound")
                {
                    await Funciones.GrabarLogError(context, $"Error en GetPersMedia /anime: {ex.Message}\n```{ex.StackTrace}```");
                }
            }
            return null;
        }

        public static async Task VincularAniList(DiscordInteraction ctx, DiscordClient client)
        {
            UsuariosAnilist usuariosAnilist = new();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AsEphemeral(true)
                .AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Configura tu perfil de AniList",
                    Description =
                        $"**Instrucciones**:\n\n" +
                        $"1- Haz click en el botón llamado **Autorizar**\n" +
                        $"2- Una vez se abra la página web, haz click en el botón verde **Authorize** y luego copia el texto que te aparecerá para copiar\n" +
                        $"3- Cierra la página web y haz click en el botón llamado **Pegar código aquí**\n" +
                        $"4- Pega el código en el formulario y envíalo",
                    Color = Funciones.GetColor()
                })
                .AddComponents(
                    new DiscordLinkButtonComponent(@"https://anilist.co/api/v2/oauth/authorize?client_id=8655&response_type=token", "Autorizar"),
                    new DiscordButtonComponent(ButtonStyle.Primary, $"modal-anilistprofileset-{ctx.User.Id}", "Pegar código aquí")
                )
            );

            DiscordMessage message = await ctx.GetOriginalResponseAsync();
            var interactivity = client.GetInteractivity();
            var interactivityBtnResult = await interactivity.WaitForButtonAsync(message, TimeSpan.FromMinutes(5));

            if (!interactivityBtnResult.TimedOut)
            {
                var btnInteraction = interactivityBtnResult.Result.Interaction;
                string modalId = $"modal-{btnInteraction.Id}";

                var modal = new DiscordInteractionResponseBuilder()
                    .WithCustomId(modalId)
                    .WithTitle("Vincular AniList")
                    .AddComponents(new TextInputComponent(label: "Código", placeholder: "Pegar código aquí", customId: "AniListToken"));

                await btnInteraction.CreateResponseAsync(InteractionResponseType.Modal, modal);

                var interactivityModalResult = await interactivity.WaitForModalAsync(modalId, TimeSpan.FromMinutes(5));

                if (!interactivityModalResult.TimedOut)
                {
                    var modalInteraction = interactivityModalResult.Result.Interaction;
                    string ALToken = interactivityModalResult.Result.Values.First().Value;

                    await modalInteraction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                    GraphQLHttpClient graphQlCli = new("https://graphql.anilist.co", new NewtonsoftJsonSerializer());
                    if (graphQlCli.HttpClient.DefaultRequestHeaders.Contains("Authorization"))
                    {
                        graphQlCli.HttpClient.DefaultRequestHeaders.Remove("Authorization");
                    }

                    graphQlCli.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ALToken}");

                    var request = new GraphQLRequest
                    {
                        Query =
                            "query {" +
                            "   Viewer {" +
                            "       id," +
                            "       name," +
                            "       siteUrl," +
                            "       avatar {" +
                            "           medium" +
                            "       }," +
                            "       bannerImage" +
                            "   }" +
                            "}"
                    };
                    try
                    {
                        var data = await graphQlCli.SendQueryAsync<dynamic>(request);
                        if (data != null)
                        {
                            if (data.Data != null)
                            {
                                int id = data.Data.Viewer.id;
                                string name = data.Data.Viewer.name;
                                string siteUrl = data.Data.Viewer.siteUrl;
                                string avatar = data.Data.Viewer.avatar.medium;
                                string banner = data.Data.Viewer.bannerImage;

                                var newProfileEmbed = new DiscordEmbedBuilder
                                {
                                    Color = DiscordColor.Green,
                                    Title = "Nuevo perfil guardado exitosamente",
                                    Description = string.Format("{0}, has guardado tu perfil de Anilist correctamente", ctx.User.Mention),
                                    Thumbnail = new()
                                    {
                                        Url = avatar
                                    },
                                    Author = new()
                                    {
                                        Url = siteUrl,
                                        Name = name,
                                        IconUrl = ctx.User.AvatarUrl
                                    }
                                };

                                if (!string.IsNullOrEmpty(banner))
                                {
                                    newProfileEmbed.WithImageUrl(banner);
                                }

                                var member = ctx.Guild.Members[ctx.User.Id];

                                await usuariosAnilist.SetAnilist(client, ctx.Guild, siteUrl, member);
                                await usuariosAnilist.SetAnilistYumiko(id, member.Id);

                                var servicios = ServiciosSingleton.GetServiciosSingleton();
                                var users = servicios.Usuarios;
                                if (!users.Where(u => (ulong)u.UserId == ctx.User.Id).Any())
                                {
                                    var newList = await usuariosAnilist.GetListaUsuarios();
                                    var newUser = newList.Find(u => (ulong)u.UserId == ctx.User.Id);
                                    users.Add(newUser);
                                    servicios.SetUsuarios(users);
                                }

                                await ctx.Guild.Channels[862408834693070901].SendMessageAsync(embed: newProfileEmbed, content: ctx.User.Mention);
                                await modalInteraction.DeleteOriginalResponseAsync();
                                await ctx.DeleteOriginalResponseAsync();

                                try
                                {
                                    var miembroRole = ctx.Guild.Roles[862452184029069332];
                                    await member.GrantRoleAsync(miembroRole);
                                }
                                catch (Exception)
                                {
                                    await modalInteraction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                                    {
                                        Title = "Error",
                                        Description = "Error desconocido agregando el rol miembro. Notificar al staff por favor.",
                                        Color = DiscordColor.Red
                                    }));
                                }

                                return;
                            }
                        }

                        await modalInteraction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Error",
                            Description = "Error desconocido",
                            Color = DiscordColor.Red
                        }));
                    }
                    catch (GraphQLHttpRequestException ex)
                    {
                        if (ex.Content != null)
                        {
                            dynamic data = JObject.Parse(ex.Content);
                            if (data.errors != null)
                            {
                                foreach (var error in data.errors)
                                {
                                    await modalInteraction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                                    {
                                        Title = "Error",
                                        Description = error.message,
                                        Color = DiscordColor.Red
                                    }));
                                }
                                return;
                            }
                        }

                        await modalInteraction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Error",
                            Description = "Error desconocido",
                            Color = DiscordColor.Red
                        }));
                    }
                }
                else
                {
                    await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Tiempo agotado esperando la respuesta",
                        Color = DiscordColor.Red
                    }));
                }
            }
            else
            {
                await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Tiempo agotado esperando la respuesta",
                    Color = DiscordColor.Red
                }));
            }
        }

        private static string FormatearScoreUJser(string scoreFormat, string scorePers)
        {
            string scoreF = string.Empty;
            switch (scoreFormat)
            {
                case "POINT_10":
                case "POINT_10_DECIMAL":
                    scoreF = $"{scorePers}/10";
                    break;
                case "POINT_100":
                    scoreF = $"{scorePers}/100";
                    break;
                case "POINT_5":
                    int scoreS = int.Parse(scorePers);
                    for (int i = 0; i < scoreS; i++)
                    {
                        scoreF += "★";
                    }

                    break;
                case "POINT_3":
                    int score3 = int.Parse(scorePers);
                    switch (score3)
                    {
                        case 1:
                            scoreF = "🙁";
                            break;
                        case 2:
                            scoreF = "😐";
                            break;
                        case 3:
                            scoreF = "🙂";
                            break;
                    }

                    break;
            }

            return scoreF;
        }

        private static decimal FormatearScoreUJser100(string scoreFormat, string scorePers)
        {
            decimal scoreIni = decimal.Parse(scorePers, new NumberFormatInfo() { NumberDecimalSeparator = "." });
            return scoreFormat switch
            {
                "POINT_10" or "POINT_10_DECIMAL" => scoreIni * 10,
                "POINT_100" => scoreIni,
                "POINT_5" => scoreIni * 20,
                "POINT_3" => scoreIni * 33,
                _ => throw new ArgumentException("No existe case del switch de FormatearScoreUJser100"),
            };
        }
    }
}
