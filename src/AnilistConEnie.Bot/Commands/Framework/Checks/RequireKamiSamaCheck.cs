using AnilistConEnie.Bot.Configuration;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace AnilistConEnie.Bot.Commands.Framework.Checks;

public sealed class RequireKamiSamaCheck(BotConfiguration config) : IContextCheck<RequireKamiSamaAttribute>
{
    public ValueTask<string?> ExecuteCheckAsync(RequireKamiSamaAttribute attribute, CommandContext context)
    {
        return context.Member is not null && context.Member.Roles.Any(r => r.Id == config.Roles.KamiSama)
            ? ValueTask.FromResult<string?>(null)
            : ValueTask.FromResult<string?>("Solo los administradores pueden usar este comando.");
    }
}
