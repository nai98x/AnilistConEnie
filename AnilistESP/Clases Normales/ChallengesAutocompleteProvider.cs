namespace AnilistESP
{
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ChallengesAutocompleteProvider : IAutocompleteProvider
    {
        private List<ChallengeFirebase> challenges = new();

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            List<DiscordAutoCompleteChoice> lista = new();

            if (challenges.Count == 0)
            {
                ChallengesDAL service = new();
                challenges = await service.GetLista();
            }

            string valor = (string)ctx.FocusedOption.Value;

            var paisesFiltrado = challenges
                                    .Where(p => p.Nombre.ToLower().Contains(valor.ToLower()))
                                    .Take(10);

            foreach (var item in paisesFiltrado)
            {
                lista.Add(new(item.Nombre, item.Nombre));
            }

            return lista;
        }
    }
}
