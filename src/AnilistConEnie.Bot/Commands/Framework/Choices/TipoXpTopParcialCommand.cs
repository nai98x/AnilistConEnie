using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Framework.Choices;

public enum TipoXpTopParcialCommand
{
    [ChoiceDisplayName("Anual")] [Description("Anual")]
    Anual,
    [ChoiceDisplayName("Mensual")] [Description("Mensual")]
    Mensual,
    [ChoiceDisplayName("Semanal")] [Description("Semanal")]
    Semanal,
    [ChoiceDisplayName("Diario")] [Description("Diario")]
    Diario
}
