using System.Diagnostics;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.UserCommands;

namespace AnilistConEnie.Bot.Observabilidad;

public class CommandExecutorTrazado : DefaultCommandExecutor
{
    public override async ValueTask ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
    {
        using Activity? activity = Telemetria.Fuente.StartActivity($"comando {context.Command.FullName}");

        if (activity is not null)
        {
            activity.SetTag("discord.command.name", context.Command.FullName);
            activity.SetTag("discord.command.type", Tipo(context));
            activity.SetTag("discord.user.id", context.User.Id.ToString());
            activity.SetTag("discord.channel.id", context.Channel.Id.ToString());
        }

        await base.ExecuteAsync(context, cancellationToken);
    }

    protected override ValueTask InvokeCommandErroredEventAsync(CommandsExtension extension, CommandErroredEventArgs eventArgs)
    {
        Activity? activity = Activity.Current;

        if (activity?.Source == Telemetria.Fuente)
        {
            if (eventArgs.Exception is ChecksFailedException or ParameterChecksFailedException)
                activity.SetTag("discord.command.checks_failed", true);
            else
                Telemetria.MarcarError(activity, eventArgs.Exception);
        }

        return base.InvokeCommandErroredEventAsync(extension, eventArgs);
    }

    private static string Tipo(CommandContext context) => context switch
    {
        UserCommandContext => "user",
        MessageCommandContext => "message",
        SlashCommandContext => "slash",
        TextCommandContext => "text",
        _ => "otro"
    };
}
