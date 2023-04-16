using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Threading.Tasks;
namespace AnilistConEnie.Commands
{
    [SlashCommandGroup("premios", "Comandos de los premios de temporada del servidor")]
    public class Premios : ApplicationCommandModule
    {
        private PremiosDAL service = new();

        [SlashCommand("lista", "Permite ver los challenges del servidor")]
        public async Task Lista(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            var premios = await service.GetListaPremios();
            string desc = string.Empty;
            if (premios.Count == 0)
            {
                desc = "(Sin premios disponibles)";
            }
            else
            {
                foreach (var premio in premios)
                {
                    desc += $"- {Formatter.MaskedUrl(premio.Nombre, new Uri(premio.Link))}\n";
                }
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Premios de temporada del servidor",
                Description = desc,
                Color = Funciones.GetColor()
            }));
        }

        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        [SlashCommand("agregar", "Agrega un nuevo premio de temporada")]
        public async Task AgregarPremioDeTemporada(InteractionContext ctx, [Option("Año", "Año del premio de temporada")] double anio, [Option("Season", "Season del premio de temporada")] Season season, [Option("Link", "Link del premio de temporada")] string link)
        {
            await ctx.DeferAsync();
            PremiosDAL premiosDb = new();

            await premiosDb.SetPremio((int)anio, season, link);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(
                new DiscordEmbedBuilder()
                    .WithTitle("Premio agregado correctamente")
                    .WithDescription($"{season.GetName()} {anio}")
                    .WithColor(DiscordColor.Green)
                )
            );
        }
    }
}
