using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Enums;

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
