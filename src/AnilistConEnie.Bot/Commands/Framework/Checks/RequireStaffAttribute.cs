using DSharpPlus.Commands.ContextChecks;

namespace AnilistConEnie.Bot.Commands.Framework.Checks;

/// <summary>Restringe el comando (o todos los de la clase) a miembros con el rol KamiSama o Colaborador.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireStaffAttribute : ContextCheckAttribute;
