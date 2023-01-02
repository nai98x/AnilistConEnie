using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            [Option("Disponible", "Si el challenge se puede realizar")] bool disponible)
        {
            await ctx.DeferAsync();
            string dispStr = disponible ? "Disponible" : "No disponible";
            await service.Set(nombre, link, disponible);
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
                foreach (var ch in challenges.GroupBy(x => x.Disponible))
                {
                    if (ch.Key == true)
                    {
                        desc += Formatter.Bold("Disponibles:\n");
                    }
                    else
                    {
                        desc += Formatter.Bold("No disponibles:\n");
                    }

                    if (ch.Any())
                    {
                        foreach (var x in ch)
                        {
                            desc += $"[{x.Nombre}]({x.Link})\n";
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
                    desc += $"- <@{x.UserId}> - **XP:** {x.Xp} {emote}\n";
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
            var challengesUser = await service.GetChallengesUsuario(ctx.User.Id);
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
                Title = $"Challenges completados por {ctx.User.Username}",
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

                ranking.ForEach(x =>
                {
                    if (lastScore != x.Xp)
                    {
                        pos++;
                    }

                    switch (pos)
                    {
                        case 1:
                            DiscordEmoji emoji1 = DiscordEmoji.FromName(ctx.Client, ":first_place:");
                            description += $"{emoji1} - <@{x.UserId}>: {x.Xp} {emote}\n";
                            break;
                        case 2:
                            DiscordEmoji emoji2 = DiscordEmoji.FromName(ctx.Client, ":second_place:");
                            description += $"{emoji2} - <@{x.UserId}>: {x.Xp} {emote}\n";
                            break;
                        case 3:
                            DiscordEmoji emoji3 = DiscordEmoji.FromName(ctx.Client, ":third_place:");
                            description += $"{emoji3} - <@{x.UserId}>: {x.Xp} {emote}\n";
                            break;
                        default:
                            description += $"{Formatter.Bold($"#{pos}")} - <@{x.UserId}>: {x.Xp} {emote}\n";
                            break;
                    }

                    lastScore = x.Xp;
                });

                description = description.Remove(description.Length - 1, 1);
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
            [Option("Xp", "XP recibida por completarlo")] double xp)
        {
            await ctx.DeferAsync();
            await service.SetUsuarioChallenge(challenge, (long)usuario.Id, (int)xp);
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Challenges completado!",
                Description = $"Felicitaciones {usuario.Mention}! Completaste el `{challenge}`",
                Color = DiscordColor.Green
            }));
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
