using DSharpPlus.Commands.ContextChecks;

namespace AnilistConEnie.Bot.Commands.Framework.Checks;

/// <summary>Restringe el comando (o todos los de la clase) a miembros con el rol KamiSama.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireKamiSamaAttribute : ContextCheckAttribute;
