using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Framework.Choices;

public enum TipoXpTopCommand
{
    [ChoiceDisplayName("Total")] [Description("Total")]
    Total,
    [ChoiceDisplayName("Mensajes")] [Description("Mensajes")]
    Mensajes,
    [ChoiceDisplayName("Intercambios")] [Description("Intercambios")]
    Intercambios,
    [ChoiceDisplayName("Eventos y actividades")] [Description("Eventos y actividades")]
    Eventos,
    [ChoiceDisplayName("Challenges")] [Description("Challenges")]
    Challenges,
    [ChoiceDisplayName("Extra por Boosting")] [Description("Extra por Boosting")]
    Booster,
    [ChoiceDisplayName("Otros")] [Description("Otros")]
    Otros
}
