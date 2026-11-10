using System.Diagnostics;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.UserCommands;
using Serilog.Context;
using Serilog.Core.Enrichers;

namespace AnilistConEnie.Bot.Observabilidad;

public class CommandExecutorTrazado : DefaultCommandExecutor
{
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

        using IDisposable contextoLogs = LogContext.Push(
            new PropertyEnricher("command", comando),
            new PropertyEnricher("command_type", tipo),
            new PropertyEnricher("user_id", context.User.Id.ToString()),
            new PropertyEnricher("channel_id", context.Channel.Id.ToString()));

        Medicion medicion = Medicion.Iniciar();

        try
        {
            await base.ExecuteAsync(context, cancellationToken);
        }
        finally
        {
            Telemetria.RegistrarEspera(activity, medicion);
            Telemetria.DuracionComandos.Record(
                medicion.TiempoActivo.TotalSeconds,
                new KeyValuePair<string, object?>("command", comando),
                new KeyValuePair<string, object?>("type", tipo),
                new KeyValuePair<string, object?>("outcome", medicion.Resultado));
        }
    }

    protected override ValueTask InvokeCommandErroredEventAsync(CommandsExtension extension, CommandErroredEventArgs eventArgs)
    {
        bool checksFallidos = eventArgs.Exception is ChecksFailedException or ParameterChecksFailedException;

        Medicion.Actual?.Resultado = checksFallidos ? Telemetria.ResultadoChecksFallidos : Telemetria.ResultadoError;

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
