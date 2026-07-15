using AnilistConEnie.Bot.Configuration;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>Arma los select de colores agrupados por categoría, respetando los límites de Discord.</summary>
public static class ColoresRangoMenu
{
    private const int MaxSelects = 5;
    private const int MaxOpciones = 25;

    public static List<DiscordSelectComponent> Selects(IEnumerable<BotConfiguration.ColorRangoConfiguration> colores, string customIdPrefix)
    {
        List<DiscordSelectComponent> selects = [];

        foreach (IGrouping<string, BotConfiguration.ColorRangoConfiguration> categoria in colores.GroupBy(x => x.Categoria))
        {
            foreach (BotConfiguration.ColorRangoConfiguration[] chunk in categoria.Chunk(MaxOpciones))
            {
                if (selects.Count == MaxSelects) return selects;

                List<DiscordSelectComponentOption> options = chunk
                    .Select(c => new DiscordSelectComponentOption(c.Nombre, c.RoleId.ToString()))
                    .ToList();
                selects.Add(new DiscordSelectComponent($"{customIdPrefix}{selects.Count + 1}", categoria.Key, options));
            }
        }

        return selects;
    }
}
