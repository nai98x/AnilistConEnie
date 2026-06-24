using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Enums;

public enum AccionXp
{
    [ChoiceDisplayName("Agregar")] [Description("Agregar")]
    Agregar,
    [ChoiceDisplayName("Quitar")] [Description("Quitar")]
    Quitar
}
