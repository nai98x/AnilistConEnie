using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Extensions;

public static class CommandContextExtensions
{
    /// <summary>
    /// Verifica que el bot haya terminado de inicializarse antes de ejecutar un comando.
    /// Si todavía no lo está, responde con un embed avisándolo y devuelve <c>false</c> para que el
    /// comando corte sin diferir ni continuar. Llamar siempre antes del defer.
    /// </summary>
    public static async Task<bool> BotInicializadoAsync(this CommandContext ctx, DiscordBotService discordBotService)
    {
        if (discordBotService.Inicializado)
        {
            return true;
        }

        DiscordEmbed embed = discordBotService.ErrorInicializacion
            ? new DiscordEmbedBuilder
            {
                Title = "Error al inicializar el bot",
                Description = "Hubo un error al inicializar el bot. Avisá a un administrador.",
                Color = DiscordColor.Red
            }
            : new DiscordEmbedBuilder
            {
                Title = "El bot se está iniciando",
                Description = "Espera unos segundos para volver a invocar el comando.",
                Color = DiscordColor.Orange,
                ImageUrl = "https://i.giphy.com/gQbVzXQQbGO7C.webp"
            };

        if (ctx is SlashCommandContext slashCtx)
        {
            await slashCtx.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
        }
        else
        {
            await ctx.RespondAsync(embed);
        }

        return false;
    }

    /// <summary>
    /// Difiere la respuesta marcándola como ephemeral (solo visible para quien ejecuta el comando).
    /// Para slash commands difiere la interacción como ephemeral; para el resto cae al defer normal.
    /// Recuerda marcar también el followup con <c>.AsEphemeral()</c>.
    /// </summary>
    public static async Task DeferEphemeralAsync(this CommandContext ctx)
    {
        if (ctx is SlashCommandContext slashCtx)
        {
            await slashCtx.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());
        }
        else
        {
            await ctx.DeferResponseAsync();
        }
    }
}
