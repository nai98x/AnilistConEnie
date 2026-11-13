using DSharpPlus.Commands.ContextChecks;

namespace AnilistConEnie.Bot.Commands.Framework.Checks;

/// <summary>Restringe el comando (o todos los de la clase) al dueño del bot (<c>Ids:OwnerId</c>).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireOwnerAttribute : ContextCheckAttribute;
