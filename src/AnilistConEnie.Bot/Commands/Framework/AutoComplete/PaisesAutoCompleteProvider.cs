using AnilistConEnie.Bot.Configuration;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Commands.Framework.AutoComplete;

public class PaisesAutoCompleteProvider : IAutoCompleteProvider
{
    public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        BotConfiguration config = context.ServiceProvider.GetRequiredService<BotConfiguration>();
        DiscordGuild? guild = context.Guild;
        if (guild is null) return ValueTask.FromResult(Enumerable.Empty<DiscordAutoCompleteChoice>());

        string valor = context.UserInput?.ToString()?.ToLowerInvariant() ?? string.Empty;

        IEnumerable<DiscordAutoCompleteChoice> choices = config.PaisTimezones
            .Select(x => guild.Roles.GetValueOrDefault(x.RoleId))
            .OfType<DiscordRole>()
            .DistinctBy(x => x.Id)
            .Where(x => x.Name.ToLowerInvariant().Contains(valor))
            .OrderBy(x => x.Name)
            .Take(25)
            .Select(x => new DiscordAutoCompleteChoice(x.Name, x.Id.ToString()))
            .ToList();

        return ValueTask.FromResult(choices);
    }
}
