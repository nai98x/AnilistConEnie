using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Extensions;

public static class CommandContextExtensions
{
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
