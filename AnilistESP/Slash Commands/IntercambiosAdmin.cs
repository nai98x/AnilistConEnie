using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System.Threading.Tasks;

namespace AnilistESP
{
    [SlashCommandGroup("organizador", "Comandos para organizador de intercambios")]
    public class IntercambiosAdmin : ApplicationCommandModule
    {
        private readonly IntercambiosDAL service = new();

        [SlashRequireUserPermissions(Permissions.ManageMessages)]
        [SlashCommand("inscripciones", "Inicia la fase de inscripcion (staff)")]
        public async Task Inscripciones(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await service.IniciarInscripcion(ctx, tipo);
        }

        [SlashRequireUserPermissions(Permissions.ManageMessages)]
        [SlashCommand("elecciones", "Inicia la fase de elección (staff)")]
        public async Task Elecciones(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await service.IniciarElecciones(ctx, tipo);
        }

        [SlashRequireUserPermissions(Permissions.ManageMessages)]
        [SlashCommand("iniciar", "Inicia el intercambio (staff)")]
        public async Task Iniciar(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await service.IniciarIntercambio(ctx, tipo);
        }

        [SlashRequireUserPermissions(Permissions.ManageMessages)]
        [SlashCommand("terminar", "Termina el intercambio (staff)")]
        public async Task Terminar(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await service.TerminarIntercambio(ctx, tipo);
        }
    }
}
