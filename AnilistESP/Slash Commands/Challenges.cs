using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
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
            [Option("Disponible", "SI el challenge se puede realizar")] bool disponible)
        {
            await ctx.DeferAsync();
            string dispStr = disponible ? "Disponible" : "No disponible";
            await service.Set(nombre, link, disponible);
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Nuevo challenge!",
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
            if(challenges.Count == 0)
            {
                desc = "(Sin challenges disponibles)";
            }
            else
            {
                foreach (var ch in challenges.GroupBy(x => x.Disponible))
                {
                    if(ch.Key == true)
                    {
                        desc += Formatter.Bold("Disponibles:\n");
                    }
                    else
                    {
                        desc += Formatter.Bold("Expirados:\n");
                    }

                    if(ch.Any())
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
                Title = "Challenges disponibles!",
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
                challenges.ForEach(x =>
                {
                    desc += $"- <@{x.UserId}> - **XP:** {x.Xp} uma points\n";
                });
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = $"Usuarios que completaron el {challenge}",
                Description = desc,
                Color = Funciones.GetColor()
            }));
        }

        //[SlashCommand("ranking", "Ranking de usuarios por cantidad de challenges completados")]
        public async Task Ranking(InteractionContext ctx)
        {
            await ctx.DeferAsync();
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
    }
}
