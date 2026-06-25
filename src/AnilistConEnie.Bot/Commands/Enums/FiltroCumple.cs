using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Enums;

public enum FiltroCumple
{
    [ChoiceDisplayName("Solo del mes")] [Description("Solo del mes")]
    SoloDelMes,
    [ChoiceDisplayName("Todos los cumpleaños")] [Description("Todos los cumpleaños")]
    Todos
}
