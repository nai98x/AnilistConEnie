using System.ComponentModel;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[Command("trigger")]
[Description("Comandos para triggers")]
//[TestCommand]
public class Triggers(ITriggersRepository triggersRepository, TriggersState triggersState, DiscordLogService logService, DiscordBotService discordBotService)
{
    [Command("set")]
    [Description("Agrega o modifica un trigger (staff)")]
    public async Task Set(
        SlashCommandContext ctx,
        [Parameter("Nombre")] [Description("Nombre para identificar al trigger")] string nombre,
        [Parameter("Tipo")] [Description("Tipo de trigger")] TipoTrigger tipo,
        [Parameter("Texto")] [Description("Texto a mostrar")] string? texto = null,
        [Parameter("Imagen")] [Description("Url de la imagen a mostrar")] string? imagen = null)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();
        if (!await EnsureManageGuildAsync(ctx)) return;

        if (string.IsNullOrEmpty(texto) && string.IsNullOrEmpty(imagen))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = $"El trigger {Formatter.InlineCode(nombre)} debe tener un texto o una imagen (o ambos)",
                Color = DiscordColor.Red
            }));
            return;
        }

        Trigger trigger = new()
        {
            Nombre = nombre.ToLower(),
            Texto = texto ?? string.Empty,
            ImageUrl = imagen ?? string.Empty,
            Tipo = (int)tipo
        };

        await triggersRepository.Upsert(trigger);
        triggersState.SetTrigger(trigger);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Trigger agregado",
            Description = $"Trigger {Formatter.InlineCode(nombre)} agregado correctamente.",
            Color = DiscordColor.Green
        }));
    }

    [Command("eliminar")]
    [Description("Desactiva un trigger (staff)")]
    public async Task Eliminar(
        SlashCommandContext ctx,
        [Parameter("Nombre")] [Description("Trigger a desactivar")] string nombre)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();
        if (!await EnsureManageGuildAsync(ctx)) return;

        bool exito = await triggersRepository.Delete(nombre.ToLower());

        if (exito)
        {
            triggersState.RemoveTriggerFromActiveList(nombre.ToLower());

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Trigger eliminado",
                Description = $"Trigger {Formatter.InlineCode(nombre)} fue eliminado correctamente.",
                Color = DiscordColor.Green
            }));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = $"Trigger {Formatter.InlineCode(nombre)} no pudo desactivarse.",
                Color = DiscordColor.Red
            }));
        }
    }

    [Command("setfrommessagetext")]
    [Description("Crea o modifica un trigger segun el texto de un mensaje (staff)")]
    public async Task SetFromMessageText(
        SlashCommandContext ctx,
        [Parameter("Nombre")] [Description("Nombre del trigger")] string nombre,
        [Parameter("Tipo")] [Description("Tipo de trigger")] TipoTrigger tipo,
        [Parameter("Canal")] [Description("Canal del mensaje referenciado")] DiscordChannel channel,
        [Parameter("IdMensaje")] [Description("Id del mensaje referenciado")] string messageId)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();
        if (!await EnsureManageGuildAsync(ctx)) return;

        if (!ulong.TryParse(messageId, out ulong messageIdUlong))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = $"Formato de idMensaje referenciado incorrecto: {Formatter.InlineCode(messageId)}",
                Color = DiscordColor.Red
            }));
            return;
        }

        try
        {
            DiscordMessage message = await channel.GetMessageAsync(messageIdUlong);

            Trigger trigger = new()
            {
                Nombre = nombre.ToLower(),
                Texto = message.Content,
                ImageUrl = string.Empty,
                Tipo = (int)tipo
            };

            await triggersRepository.Upsert(trigger);
            triggersState.SetTrigger(trigger);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Trigger agregado",
                Description = $"Trigger {Formatter.InlineCode(nombre)} agregado correctamente.",
                Color = DiscordColor.Green
            }));
        }
        catch (Exception ex)
        {
            await logService.GrabarLogGeneralError(ctx.Guild!, $"Error en SetFromMessageText:\n\n{ex.Message}:\n{Formatter.BlockCode(ex.StackTrace ?? string.Empty)}");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = $"Trigger {Formatter.InlineCode(nombre)} no pudo crearse.",
                Color = DiscordColor.Red
            }));
        }
    }

    [Command("lista")]
    [Description("Ve los triggers")]
    public async Task Lista(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        IReadOnlyDictionary<string, Trigger> activeTriggers = triggersState.GetActiveTriggers();

        if (activeTriggers.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Sin registros",
                Description = "No hay ningun trigger registrado!",
                Color = DiscordColor.Red
            }));
            return;
        }

        string desc = string.Empty;
        foreach (IGrouping<int, KeyValuePair<string, Trigger>> tipo in activeTriggers.GroupBy(x => x.Value.Tipo).OrderBy(x => x.Key))
        {
            string tipoNombre = ((TipoTrigger)tipo.Key).GetDescription();
            desc += $"**{tipoNombre}**:\n" +
                    $"- {string.Join(", ", tipo.Select(x => $"`{x.Key}`"))}";
            desc += "\n\n";
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Triggers del server",
            Description = StringHelper.NormalizarDescription(desc),
            Color = DiscordColor.Green
        }));
    }

    private static async Task<bool> EnsureManageGuildAsync(SlashCommandContext ctx)
    {
        if (ctx.Member is not null && ctx.Member.Permissions.HasPermission(DiscordPermission.ManageGuild))
            return true;

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Sin permiso",
            Description = "Necesitas el permiso de `Gestionar servidor` para usar este comando.",
            Color = DiscordColor.Red
        }));
        return false;
    }
}
