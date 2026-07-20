using AnilistConEnie.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Commands.Framework;

public sealed class CommandErrorHandler(DiscordLogService logService)
{
    public async Task HandleAsync(CommandsExtension _, CommandErroredEventArgs args)
    {
        DiscordEmbedBuilder embed;

        if (args.Exception is ChecksFailedException checks)
        {
            string mensaje = string.Join("\n", checks.Errors
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m)));

            embed = ErrorEmbed.De("Sin permiso",
                string.IsNullOrWhiteSpace(mensaje) ? "No tienes permiso para usar este comando." : mensaje);
        }
        else
        {
            embed = ErrorEmbed.De("Ocurrió un error inesperado al ejecutar el comando.");
            if (args.Context.Guild is not null)
                await logService.LogException(args.Context.Guild, args.Exception, $"Comando {args.Context.Command.FullName}");
        }

        await ResponderAsync(args.Context, embed);
    }

    private static async ValueTask ResponderAsync(CommandContext ctx, DiscordEmbedBuilder embed)
    {
        DiscordMessageBuilder message = new DiscordMessageBuilder().AddEmbed(embed);

        if (ctx is SlashCommandContext { Interaction.ResponseState: not DiscordInteractionResponseState.Unacknowledged })
            await ctx.FollowupAsync(message);
        else
            await ctx.RespondAsync(message);
    }
}
