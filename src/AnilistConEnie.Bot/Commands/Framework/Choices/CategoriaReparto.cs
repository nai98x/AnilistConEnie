using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Framework.Choices;

public enum CategoriaReparto
{
    [ChoiceDisplayName("Animes")] [Description("Animes")]
    Animes,
    [ChoiceDisplayName("Mangas")] [Description("Mangas")]
    Mangas,
    [ChoiceDisplayName("Pelis")] [Description("Pelis")]
    Pelis,
    [ChoiceDisplayName("Series")] [Description("Series")]
    Series,
    [ChoiceDisplayName("Música")] [Description("Música")]
    Musica,
    [ChoiceDisplayName("Fanarts")] [Description("Fanarts")]
    Fanarts,
    [ChoiceDisplayName("Sala del anime")] [Description("Sala del anime")]
    SalaDelAnime,
    [ChoiceDisplayName("Otros")] [Description("Otros")]
    Otros
}
