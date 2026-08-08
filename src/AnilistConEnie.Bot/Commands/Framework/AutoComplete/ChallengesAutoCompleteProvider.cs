using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Commands.Framework.AutoComplete;

public class ChallengesAutoCompleteProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        IChallengesRepository challengesRepository = context.ServiceProvider.GetRequiredService<IChallengesRepository>();
        List<Challenge> challenges = await challengesRepository.GetLista();

        string valor = context.UserInput?.ToString()?.ToLowerInvariant() ?? string.Empty;

        return challenges
            .Where(x => x.Nombre.ToLowerInvariant().Contains(valor))
            .Take(10)
            .Select(x => new DiscordAutoCompleteChoice(x.Nombre, x.Nombre));
    }
}
