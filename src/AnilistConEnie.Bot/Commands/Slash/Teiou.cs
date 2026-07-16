using System.ComponentModel;
using System.Globalization;
using AnilistConEnie.Bot.Commands.Framework.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;

namespace AnilistConEnie.Bot.Commands.Slash;

[Command("teiou")]
[Description("Comandos para rango teiou")]
//[TestCommand]
public class Teiou(CooldownsSettings cooldownsSettings, RangoRoles rangoRoles, ITeiouCooldownRepository teiouCooldownRepository, DiscordBotService discordBotService)
{
    [Command("nickname")]
    [Description("Cambia el nickname de una persona")]
    public async Task Nickname(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("Usuario a cambiar el nickname")] DiscordUser user,
        [Parameter("Nickname")] [Description("Nuevo nickname")] string nickname)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync(true);

        if (ctx.Member is null || !rangoRoles.RangoAPartirDe(ctx.Guild!, ctx.Member, RangoEnum.Teiou, false))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Sin permiso")
                .WithDescription("Solo los miembros con el rango **Teiou** pueden usar este comando.")
                .WithColor(DiscordColor.Red)));
            return;
        }

        DateTime? cooldown = await teiouCooldownRepository.Obtener(ctx.User.Id);

        if (cooldown is null || cooldown.Value <= DateTime.UtcNow)
        {
            DiscordMember member = await ctx.Guild!.GetMemberAsync(user.Id);
            string oldNickname = member.Nickname ?? member.DisplayName;

            await teiouCooldownRepository.Upsert(ctx.User.Id, DateTime.UtcNow.AddHours(cooldownsSettings.TeiouApodoHoras));

            await member.ModifyAsync(x => x.Nickname = nickname);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Nickname modificado")
                .WithDescription($"Se modificó el nickname de {oldNickname} a {member.Mention}")
                .WithColor(DiscordColor.Green)));
        }
        else
        {
            NumberFormatInfo nfi = new CultureInfo("es-ES", false).NumberFormat;
            nfi.NumberDecimalSeparator = ",";
            nfi.NumberGroupSeparator = ".";
            nfi.NumberDecimalDigits = 0;

            double horas = (cooldown.Value - DateTime.UtcNow).TotalHours;

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"Debes esperar {horas.ToString("N", nfi)} horas para volver a utilizar el comando")
                .WithColor(DiscordColor.Red)));
        }
    }
}
