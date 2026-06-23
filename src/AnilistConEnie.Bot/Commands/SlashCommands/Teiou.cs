using System.ComponentModel;
using System.Globalization;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[Command("teiou")]
[Description("Comandos para rango teiou")]
//[TestCommand]
public class Teiou(TeiouCooldownState teiouCooldownState, DiscordHelper discordHelper, IUsuariosDiscordRepository usuariosDiscordRepository)
{
    [Command("nickname")]
    [Description("Cambia el nickname de una persona")]
    public async Task Nickname(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("Usuario a cambiar el nickname")] DiscordUser user,
        [Parameter("Nickname")] [Description("Nuevo nickname")] string nickname)
    {
        await ctx.DeferResponseAsync(true);

        if (ctx.Member is null || !discordHelper.RangoAPartirDe(ctx.Guild!, ctx.Member, RangoEnum.Teiou, false))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Sin permiso")
                .WithDescription("Solo los miembros con el rango **Teiou** pueden usar este comando.")
                .WithColor(DiscordColor.Red)));
            return;
        }

        if (!teiouCooldownState.TeiouInCooldown(ctx.User.Id))
        {
            DiscordMember member = await ctx.Guild!.GetMemberAsync(user.Id);
            string oldNickname = member.Nickname ?? member.DisplayName;

            teiouCooldownState.AddTeiouCooldown(ctx.User.Id);
            await usuariosDiscordRepository.SetCooldownTeiou(ctx.User.Id);

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

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"Debes esperar {teiouCooldownState.GetHoursCooldownTeiou(ctx.User.Id).ToString("N", nfi)} horas para volver a utilizar el comando")
                .WithColor(DiscordColor.Red)));
        }
    }
}
