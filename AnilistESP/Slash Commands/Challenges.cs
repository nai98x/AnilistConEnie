using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomat.TatsuSharp;

namespace AnilistConEnie.Commands
{
    [SlashCommandGroup("challenges", "Comandos de los challenges del servidor")]
    public class Challenges : ApplicationCommandModule
    {
        private ChallengesDAL service = new();

        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        [SlashCommand("set", "Agrega o modifica un challenge (Staff)")]
        public async Task Set(InteractionContext ctx,
            [Option("Nombre", "Nombre del challenge")] string nombre,
            [Option("Link", "Link del challenge")] string link,
            [Option("Disponible", "Si el challenge se puede realizar")] bool disponible,
            [Option("Vencimiento", "Vencimiento del challenge")] string? vencimiento = null
            )
        {
            await ctx.DeferAsync();
            string dispStr = disponible ? "Disponible" : "No disponible";
            
            if (!string.IsNullOrEmpty(vencimiento))
            {
                bool validDate = DateTime.TryParseExact(vencimiento, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fchVnc);

                if (!validDate)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Error",
                        Description = $"Fecha `{vencimiento}` invalida (debe ser dd/MM/yyyy",
                        Color = DiscordColor.Red
                    }));
                    return;
                }

                await service.Set(nombre, link, disponible, fchVnc);
            }
                
            else
            {
                await service.Set(nombre, link, disponible, null);
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Nuevo challenge creado",
                Description = $"[{nombre}]({link}) ({dispStr})",
                Color = DiscordColor.Green
            }));
        }

        [SlashCommand("lista", "Permite ver los challenges del servidor")]
        public async Task Lista(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            var challenges = await service.GetLista();
            string desc = string.Empty;
            if (challenges.Count == 0)
            {
                desc = "(Sin challenges disponibles)";
            }
            else
            {
                var challengesToPrint = new Dictionary<string, List<ChallengeFirebase>>();

                var challengesByDisponible = challenges.GroupBy(x => x.Disponible);
                challengesByDisponible = challengesByDisponible.OrderByDescending(x => x.Key);

                foreach (var ch in challengesByDisponible)
                {
                    if (ch.Key == true)
                    {
                        var challengesByVencimiento = ch.GroupBy(x => x.Vencimiento != null);
                        foreach (var chh in challengesByVencimiento)
                        {
                            if (chh.Key == true)
                            {
                                challengesToPrint.Add("Por tiempo limitado", chh.ToList());
                            }
                            else
                            {
                                challengesToPrint.Add("Disponibles", chh.ToList());
                            }
                        }
                    }
                    else
                    {
                        challengesToPrint.Add("No disponibles", ch.ToList());
                    }
                }

                foreach (var challenge in challengesToPrint)
                {
                    desc += Formatter.Bold(challenge.Key) + ":\n";

                    var ch = challenge.Value;
                    if (ch.Any())
                    {
                        foreach (var x in ch)
                        {
                            desc += $"[{x.Nombre}]({x.Link})";
                            if (x.Vencimiento.HasValue && x.Disponible)
                            {
                                var dt = x.Vencimiento.Value;
                                desc += $" (hasta **{dt.Day}/{dt.Month}/{dt.Year}**)";
                            }
                            desc += "\n";
                        }
                    }
                    else
                    {
                        desc += "(Sin registros)\n";
                    }

                    desc += "\n";
                }
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Challenges del servidor",
                Description = desc,
                Color = Funciones.GetColor()
            }));
        }

        [SlashCommand("ver", "Permite ver los usuarios que han completado un challenge")]
        public async Task Ver(
            InteractionContext ctx,
            [Option("Challenge", "Challenge a elegir", true)][Autocomplete(typeof(ChallengesAutocompleteProvider))] string challenge)
        {
            await ctx.DeferAsync();
            var challenges = await service.GetListaUsuariosCompletaron(challenge);
            string desc = string.Empty;
            if (challenges.Count == 0)
            {
                desc = "(Ningún usuario ha completado este challenge)";
            }
            else
            {
                var emote = DiscordEmoji.FromGuildEmote(ctx.Client, 862461175950606376);
                challenges.ForEach(x =>
                {
                    if (ctx.Guild.Members.TryGetValue((ulong)x.UserId, out _))
                    {
                        desc += $"- <@{x.UserId}> - **XP:** {x.Xp} {emote}\n";
                    }
                });
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = $"Usuarios que completaron el {challenge}",
                Description = desc,
                Color = Funciones.GetColor()
            }));
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Challenges")]
        public async Task Usuario(ContextMenuContext ctx)
        {
            await ctx.DeferAsync();
            var challengesUser = await service.GetChallengesUsuario(ctx.TargetMember.Id);
            string description = string.Empty;
            if (challengesUser.Count > 0)
            {
                var emote = DiscordEmoji.FromGuildEmote(ctx.Client, 862461175950606376);
                int xpTotal = 0;
                challengesUser.ForEach(x =>
                {
                    description += $"[{x.Challenge.Nombre}]({x.Challenge.Link}) - {x.Xp}\n";
                    xpTotal += x.Xp;
                });

                description += $"\nTotal de XP obtenida: **{xpTotal}** {emote}";
            }
            else
            {
                description = "(Ningún challenge completado)";
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = $"Challenges completados por {ctx.TargetMember.DisplayName}",
                Description = description,
                Color = Funciones.GetColor()
            }));
        }

        [SlashCommand("ranking", "Ranking de usuarios por cantidad de challenges completados")]
        public async Task Ranking(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            var ranking = await service.GetRankingUsuarios();

            string description = string.Empty;
            if (ranking.Count > 0)
            {
                var emote = DiscordEmoji.FromGuildEmote(ctx.Client, 862461175950606376);
                int pos = 0;
                int lastScore = 0;
                int? myPos = null;

                ranking.ForEach(x =>
                {
                    if (ctx.Guild.Members.TryGetValue((ulong)x.UserId, out var member))
                    {
                        if (lastScore != x.Xp)
                        {
                            pos++;
                        }

                        if ((ulong)x.UserId == ctx.User.Id) myPos = pos;

                        switch (pos)
                        {
                            case 1:
                                DiscordEmoji emoji1 = DiscordEmoji.FromName(ctx.Client, ":first_place:");
                                description += $"{emoji1} - **{member.DisplayName}**: {x.Xp} {emote}\n";
                                break;
                            case 2:
                                DiscordEmoji emoji2 = DiscordEmoji.FromName(ctx.Client, ":second_place:");
                                description += $"{emoji2} - **{member.DisplayName}**: {x.Xp} {emote}\n";
                                break;
                            case 3:
                                DiscordEmoji emoji3 = DiscordEmoji.FromName(ctx.Client, ":third_place:");
                                description += $"{emoji3} - **{member.DisplayName}**: {x.Xp} {emote}\n";
                                break;
                            default:
                                description += $"{Formatter.Bold($"#{pos}")} - **{member.DisplayName}**: {x.Xp} {emote}\n";
                                break;
                        }

                        lastScore = x.Xp;
                    }
                });

                description = description.Remove(description.Length - 1, 1);

                if (myPos != null)
                {
                    description = $"**Tu posición es #{myPos}\n\n" + description;
                }
            }
            else
            {
                description = "(Ningún usuario ha completado ningún challenge)";
            }

            var interactivity = ctx.Client.GetInteractivity();

            var builder = new DiscordEmbedBuilder
            {
                Title = "Ranking de challenges de usuarios",
                Color = Funciones.GetColor(),
            };
            var pages = interactivity.GeneratePagesInEmbed(description, SplitType.Line, builder);
            await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, pages, asEditResponse: true);
        }

        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        [SlashCommand("completar", "Agrega un challenge a un usuario (Staff)")]
        public async Task Completar(
            InteractionContext ctx,
            [Option("Challenge", "Challenge a elegir", true)][Autocomplete(typeof(ChallengesAutocompleteProvider))] string challenge,
            [Option("Usuario", "Usuario que completo el challenge")] DiscordUser usuario,
            [Option("Xp", "XP recibida por completarlo")] double xp,
            [Option("Imagen", "URL de la imagen de la placa del challenge")] string imagen1,
            [Option("Imagen2", "URL de la imagen de la placa del challenge")] string imagen2 = null,
            [Option("Imagen3", "URL de la imagen de la placa del challenge")] string imagen3 = null
            )
        {
            await ctx.DeferAsync();

            string token = await Funciones.ObtenerTokenTatsu();
            bool updatedTatsuPoints = false;
            var client = new RestClient("https://api.tatsu.gg/v1");
            var request = new RestRequest($"/guilds/{ctx.Guild.Id}/members/{usuario.Id}/score", Method.Patch);
            request.AddHeader("Authorization", token);
            request.AddHeader("Content-Type", "application/json");

            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(new { action = 0, amount = (int)xp });

            try
            {
                var response = await client.ExecuteAsync(request);

                if(response.IsSuccessStatusCode)
                {
                    updatedTatsuPoints= true;
                }
                else
                {
                    await Funciones.GrabarLogError(Funciones.GetContext(ctx), $"Error agregando puntos de Tatsu\n{response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                await Funciones.GrabarLogError(Funciones.GetContext(ctx), $"Error agregando puntos de Tatsu\n{ex.Message}\n{Formatter.BlockCode(ex.StackTrace)}");
            }

            DiscordEmoji umaPoints = DiscordEmoji.FromGuildEmote(ctx.Client, 862461175950606376);
            string description = $"¡Felicitaciones {usuario.Mention}! Completaste el `{challenge}`";
            if (updatedTatsuPoints) description += $"y ganaste **{xp} {umaPoints} de xp**.";

            await service.SetUsuarioChallenge(challenge, (long)usuario.Id, (int)xp);

            var builder = new DiscordFollowupMessageBuilder()
                .WithContent($"<@{usuario.Id}>")
                .AddMention(new UserMention(usuario))
                .AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Challenges completado",
                    Description = description,
                    Color = DiscordColor.Green,
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = "https://media.discordapp.net/attachments/862568630365323264/990747470508204032/unknown.png"
                    }
                }
            );

            builder.AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Blurple,
                ImageUrl = imagen1
            });

            if (imagen2 != null)
            {
                builder.AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Blurple,
                    ImageUrl = imagen2
                });
            }

            if (imagen3 != null)
            {
                builder.AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Blurple,
                    ImageUrl = imagen3
                });
            }

            await ctx.FollowUpAsync(builder);
        }

        [SlashCommand("numero", "Saca un numero aleatorio entre el 1 y el 10")]
        public async Task Numero(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            Random rnd = new((int)ctx.User.Id);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Numero aleatorio",
                Description = $"Tu numero es `{rnd.Next(1, 10)}`",
                Color = DiscordColor.Blurple
            }));
        }
    }
}
