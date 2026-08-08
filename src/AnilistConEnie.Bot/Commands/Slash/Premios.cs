using System.ComponentModel;
using AnilistConEnie.Application.Premios;
using AnilistConEnie.Bot.Commands.Framework.Attributes;
using AnilistConEnie.Bot.Commands.Framework.Checks;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;

namespace AnilistConEnie.Bot.Commands.Slash;

[Command("premios")]
[Description("Comandos de los premios de temporada del servidor")]
//[TestCommand]
public class Premios(IPremiosRepository premiosRepository, DiscordBotService discordBotService)
{
    [Command("lista")]
    [Description("Permite ver los premios de temporada del servidor")]
    public async Task Lista(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        List<Premio> premios = await premiosRepository.GetLista();
        string desc;

        if (premios.Count == 0)
        {
            desc = "(Sin premios disponibles)";
        }
        else
        {
            desc = string.Empty;
            foreach (IGrouping<int, Premio> premioPorAnio in premios.GroupBy(x => x.Year).OrderBy(y => y.Key))
            {
                desc += $"**{premioPorAnio.Key}:**\n";
                foreach (Premio season in premioPorAnio.OrderBy(x => x.Order))
                    desc += $"- {Formatter.MaskedUrl($"{(Season)season.Order}", new Uri(season.Link))}\n";
                desc += "\n";
            }
        }

        await ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Premios de temporada del servidor",
            Description = desc,
            Color = DiscordEmojiHelper.GetColor(),
            Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = "Si no puedes acceder a los links debes agregar la actividad \"Contenido extra\""
            }
        }));
    }

    [Command("agregar")]
    [Description("Agrega un nuevo premio de temporada (Staff)")]
    [RequireKamiSama]
    public async Task Agregar(
        SlashCommandContext ctx,
        [Parameter("Anio")] [Description("Año del premio de temporada")] int anio,
        [Parameter("Season")] [Description("Season del premio de temporada")] Season season,
        [Parameter("Link")] [Description("Link del premio de temporada")] string link)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await premiosRepository.Upsert(PremioFactory.Crear(anio, season, link));

        await ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithTitle("Premio agregado correctamente")
            .WithDescription($"{season} {anio}")
            .WithColor(DiscordColor.Green)));
    }
}
