namespace AnilistConEnie.Bot.Commands.Framework.Attributes;

/// <summary>
/// Marca una clase de slash command para que se registre en compilaciones de DEBUG (bot de test).
/// En RELEASE (prod) se registran todos los comandos del namespace independientemente de este atributo.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TestCommandAttribute : Attribute;
