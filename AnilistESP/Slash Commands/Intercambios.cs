using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AnilistESP
{
    [SlashCommandGroup("intercambio", "Comandos para intercambios")]
    public class Intercambios : ApplicationCommandModule
    {
        private readonly IntercambiosDAL service = new();

        [SlashCommand("inscribirse", "Te inscribes en el intercambio")]
        public async Task Inscribirse(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo, [Option("Preferencia_1", "Preferencia 1")] string pref1 = null, [Option("Preferencia_2", "Preferencia 2")] string pref2 = null, [Option("Ban", "Baneo")] string ban = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await service.InscribirseIntercambio(ctx, tipo, pref1, pref2, ban);
        }

        [SlashCommand("desinscribirse", "Te inscribes en el intercambio")]
        public async Task Desinscribirse(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
            await service.DesinscribirseIntercambio(ctx, tipo);
        }

        [SlashCommand("reveal", "Ves a quien le tienes que recomendar en el intercambio")]
        public async Task Reveal(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
            await service.RevealIntercambio(ctx, tipo);
        }

        [SlashCommand("recomendar", "Realizas la recomendación en el intermcabio")]
        public async Task Recomendar(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo, [Option("Recomendacion", "Obra a recomendar")] string media)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
            await service.RecomendarIntercambio(ctx, tipo, media);
        }

        [SlashCommand("obtener", "Obtiene tu anime recomendado")]
        public async Task Recomendado(InteractionContext ctx, [Choice("Anime", "Anime")][Choice("Manga", "Manga")][Option("Tipo", "Tipo de intercambio")] string tipo)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
            await service.GetRecomendado(ctx, tipo);
        }
    }
}
