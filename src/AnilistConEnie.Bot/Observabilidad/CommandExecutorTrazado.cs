using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    // El resultado lo decide InvokeCommandErroredEventAsync, que corre dentro de base.ExecuteAsync:
    // el AsyncLocal baja hacia esa llamada y la caja mutable deja que el valor suba de vuelta.
    private static readonly AsyncLocal<StrongBox<string>?> ResultadoActual = new();

    public override async ValueTask ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
    {
        string comando = context.Command.FullName;
        string tipo = Tipo(context);

        using Activity? activity = Telemetria.Fuente.StartActivity($"comando {comando}");

        if (activity is not null)
        {
            activity.SetTag("discord.command.name", comando);
            activity.SetTag("discord.command.type", tipo);
            activity.SetTag("discord.user.id", context.User.Id.ToString());
            activity.SetTag("discord.channel.id", context.Channel.Id.ToString());
        }

        StrongBox<string> resultado = new(Telemetria.ResultadoOk);
        ResultadoActual.Value = resultado;
        long inicio = Stopwatch.GetTimestamp();

        try
        {
            await base.ExecuteAsync(context, cancellationToken);
        }
        finally
        {
            Telemetria.DuracionComandos.Record(
                Stopwatch.GetElapsedTime(inicio).TotalSeconds,
                new KeyValuePair<string, object?>("command", comando),
                new KeyValuePair<string, object?>("type", tipo),
                new KeyValuePair<string, object?>("outcome", resultado.Value));
        }
    }

    protected override ValueTask InvokeCommandErroredEventAsync(CommandsExtension extension, CommandErroredEventArgs eventArgs)
    {
        bool checksFallidos = eventArgs.Exception is ChecksFailedException or ParameterChecksFailedException;

        ResultadoActual.Value?.Value = checksFallidos ? Telemetria.ResultadoChecksFallidos : Telemetria.ResultadoError;

        Activity? activity = Activity.Current;
        if (activity?.Source == Telemetria.Fuente)
        {
            if (checksFallidos)
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
