namespace AnilistESP
{
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class PremiosAutocompleteProvider : IAutocompleteProvider
    {
        private List<PremioFirebase> premios = new();

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            List<DiscordAutoCompleteChoice> lista = new();

            if (premios.Count == 0)
            {
                PremiosDAL service = new();
                premios = await service.GetListaPremios();
            }

            string valor = (string)ctx.FocusedOption.Value;

            var paisesFiltrado = premios
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
