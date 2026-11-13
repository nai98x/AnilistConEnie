using AnilistConEnie.Bot.Configuration;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace AnilistConEnie.Bot.Commands.Framework.Checks;

public sealed class RequireOwnerCheck(BotConfiguration config) : IContextCheck<RequireOwnerAttribute>
{
    public ValueTask<string?> ExecuteCheckAsync(RequireOwnerAttribute attribute, CommandContext context)
    {
        return context.User.Id == config.OwnerId
            ? ValueTask.FromResult<string?>(null)
            : ValueTask.FromResult<string?>("Solo el dueño del bot puede usar este comando.");
    }
}
