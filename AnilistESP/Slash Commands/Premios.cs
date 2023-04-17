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
                foreach (var premioPorAnio in premios.GroupBy(x => x.Year).OrderBy(y => y.Key))
                {
                    desc += $"**{premioPorAnio.Key}:**\n";
                    foreach(var season in premioPorAnio.OrderBy(x => x.Order)) 
                    {
                        desc += $"- {Formatter.MaskedUrl($"{(Season)season.Order}", new Uri(season.Link))}\n";
                    }
                    desc += "\n";
                }
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Premios de temporada del servidor",
                Description = desc,
                Color = Funciones.GetColor(),
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = "Si no pyuedes acceder a los links debes agregar la actividad \"Contenido extra\""
                }
            }));
        }

        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        [SlashCommand("agregar", "Agrega un nuevo premio de temporada")]
        public async Task AgregarPremioDeTemporada(InteractionContext ctx, [Option("Año", "Año del premio de temporada")] double anio, [Option("Season", "Season del premio de temporada")] Season season, [Option("Link", "Link del premio de temporada")] string link)
        {
            await ctx.DeferAsync();

            await service.SetPremio((int)anio, season, link);

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
