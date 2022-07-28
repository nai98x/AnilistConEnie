using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistConEnie.Commands
{
    [SlashCommandGroup("highlights", "Comandos para highlights")]
    public class Highlights : ApplicationCommandModule
    {
        private HighlightsDAL highlightService = new();

        [SlashCommand("agregar", "Agrega una nueva palabra a tus highlights")]
        public async Task Agregar(InteractionContext ctx, [Option("Palabra", "Palabra que deseas agregar")] string palabra)
        {
            await ctx.DeferAsync(true);

            if (await UnaPalabra(ctx, palabra))
            {
                ServiciosSingleton services = ServiciosSingleton.GetServiciosSingleton();
                await highlightService.SetHighlight(ctx.User.Id, palabra);
                services.AddHighlightedWord(ctx.User.Id, palabra);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Highlight agregado",
                    Description = $"Tu highlight {Formatter.InlineCode(palabra)} fue agregado correctamente.",
                    Color = DiscordColor.Green
                }));
            }
        }

        [SlashCommand("quitar", "Elimina una palabra de tus highlights")]
        public async Task Quitar(InteractionContext ctx, [Option("Palabra", "Palabra que deseas agregar")] string palabra)
        {
            await ctx.DeferAsync(true);

            if (await UnaPalabra(ctx, palabra))
            {
                ServiciosSingleton services = ServiciosSingleton.GetServiciosSingleton();
                await highlightService.RemoveHighlight(ctx, palabra);
                services.RemoveHighlightedWord(ctx.User.Id, palabra);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Highlight eliminado",
                    Description = $"Tu highlight {Formatter.InlineCode(palabra)} fue eliminado correctamente.",
                    Color = DiscordColor.Red
                }));
            }
        }

        [SlashCommand("listar", "Ve tus highlights")]
        public async Task Listar(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);
            ServiciosSingleton services = ServiciosSingleton.GetServiciosSingleton();

            var tieneHighlights = services.GetHighlightedWords().TryGetValue(ctx.User.Id, out var highlights);
            if (tieneHighlights)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Tus highlights",
                    Description = Funciones.NormalizarDescription(string.Join("\n", highlights.Select(x => x))),
                    Color = DiscordColor.Green
                }));
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Sin registros",
                    Description = "No tienes ningun highlight registrado!",
                    Color = DiscordColor.Red
                }));
            }
        }

        private async Task<bool> UnaPalabra(InteractionContext ctx, string texto)
        {
            bool unaPalabra = texto.Split(" ").ToList().Count == 1;

            if (!unaPalabra)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = "Solo puedes ingresar una palabra",
                    Color = DiscordColor.Red
                }));
            }

            return unaPalabra;
        }
    }
}
