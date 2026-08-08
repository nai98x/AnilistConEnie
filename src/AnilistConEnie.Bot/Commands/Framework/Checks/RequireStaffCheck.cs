using AnilistConEnie.Bot.Configuration;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace AnilistConEnie.Bot.Commands.Framework.Checks;

public sealed class RequireStaffCheck(BotConfiguration config) : IContextCheck<RequireStaffAttribute>
{
    public ValueTask<string?> ExecuteCheckAsync(RequireStaffAttribute attribute, CommandContext context)
    {
        return context.Member is not null && context.Member.Roles.Any(r => r.Id == config.Roles.KamiSama || r.Id == config.Roles.Colaborador)
            ? ValueTask.FromResult<string?>(null)
            : ValueTask.FromResult<string?>("Solo un Kami Sama o un Colaborador puede usar este comando.");
    }
}
