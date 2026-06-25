using System.ComponentModel;
using System.Globalization;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Extensions.DependencyInjection;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[TestCommand]
public class Usuarios(
    IUsuariosDiscordRepository usuariosDiscordRepository,
    XpState xpState,
    BotConfiguration config,
    DiscordLogService logService,
    IHttpClientFactory httpClientFactory,
    DiscordBotService discordBotService)
{
    [Command("birthdays")]
    [Description("Muestra los cumpleaños de los usuarios")]
    public async Task Birthdays(
        SlashCommandContext ctx,
        [Parameter("Mes")] [Description("Si quieres ver los cumpleaños del mes o todos los registrados")] bool month)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<UserCumple> lista = await usuariosDiscordRepository.GetBirthdays(month);
        List<UserCumple> hoy = await usuariosDiscordRepository.GetBirthdaysHoy();

        string desc = string.Empty;

        if (hoy.Count > 0)
        {
            desc += "**Cumplen años hoy:**\n";
            foreach (UserCumple user in hoy)
            {
                if (ctx.Guild!.Members.TryGetValue((ulong)user.Id, out DiscordMember? miembro))
                    desc += $"- **{miembro.DisplayName}**\n";
            }
            desc += "\n";
        }

        desc += month ? "**Cumplen años en el próximo mes:**\n" : "**Cumplen años próximamente:**\n";

        CultureInfo es = CultureInfo.CreateSpecificCulture("es");
        foreach (UserCumple user in lista.OrderBy(x => x.BirthdayActual))
        {
            if (!ctx.Guild!.Members.TryGetValue((ulong)user.Id, out DiscordMember? miembro)) continue;

            string dia = user.FechaOriginal.ToString("dddd", es);
            string mes = user.FechaOriginal.ToString("MMMM", es);
            desc += $"- **{miembro.DisplayName}** - Cumple el {dia} {user.FechaOriginal.Day} de {mes}\n";
        }

        if (string.IsNullOrEmpty(desc))
            desc = "(No hay ningún usuario registrado que cumpla años este mes)\n";

        DiscordEmbedBuilder embed = new()
        {
            Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"Invocado por {ctx.Member!.DisplayName} ({ctx.Member.Username})",
                IconUrl = ctx.Member.AvatarUrl
            },
            Color = DiscordEmojiHelper.GetColor(),
            Title = "Cumpleaños"
        };

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        IEnumerable<Page> pages = InteractivityExtension.GeneratePagesInEmbed(desc, SplitType.Line, embed);
        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, ephemeral: false, ctx.User, pages);
    }

    [Command("setbirthday")]
    [Description("Agrega o modifica tu cumpleaños")]
    public async Task SetBirthday(
        SlashCommandContext ctx,
        [Parameter("Day")] [Description("Dia")] int day,
        [Parameter("Month")] [Description("Mes")] int month)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        BotConfiguration.PaisTimezoneConfiguration? userTimezone =
            config.PaisTimezones.FirstOrDefault(x => ctx.Member!.Roles.Any(y => y.Id == x.RoleId));

        if (userTimezone is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = "Debes tener asignado un rol de país antes de registrar tu cumpleaños.",
                Color = DiscordColor.Red
            }));
            return;
        }

        DateTime localDate = new(DateTime.Now.Year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime localDateToUtc;
        try
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(userTimezone.Timezone);
            localDateToUtc = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            localDateToUtc = localDate;
        }

        DateTime fecha = new(year: 2023, month: month, day: day, hour: 3, minute: 0, second: 0);

        await usuariosDiscordRepository.SetBirthday(ctx.Member!.Id, localDateToUtc, false, fecha);

        DiscordWebhookBuilder builder = new();
        builder.AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Cumpleaños registrado con éxito",
            Description = "Tu cumpleaños ha sido ingresado",
            Color = DiscordColor.Green
        });

        UserXp rango = xpState.GetUserXp(ctx.Member.Id);
        long xpRangoNecesario = RangoXp.XpRequerida(RangoEnum.Casual);
        if (rango.Total < xpRangoNecesario)
        {
            builder.AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Advertencia",
                Description = $"El aviso de cumpleaños no saldrá hasta llegar a rango {((Enum)RangoEnum.Casual).GetDescription()} ({xpRangoNecesario})",
                Color = DiscordColor.Yellow
            });
        }

        await ctx.EditResponseAsync(builder);
    }

    [Command("deletebirthday")]
    [Description("Elimina tu cumpleaños")]
    public async Task DeleteBirthday(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync(true);

        await usuariosDiscordRepository.DeleteBirthday(ctx.Member!.Id);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Cumpleaños eliminado",
            Description = "Tu cumpleaños ha sido eliminado del servidor",
            Color = DiscordColor.Green
        }));

        try
        {
            DiscordRole bdayRole = ctx.Guild!.Roles[config.Roles.Cumple];
            if (ctx.Member.Roles.Contains(bdayRole))
                await ctx.Member.RevokeRoleAsync(bdayRole);
        }
        catch (Exception ex) { await logService.LogException(ctx.Guild!, ex, "DeleteBirthday - revocar rol cumpleaños"); }
    }

    [Command("fundadores")]
    [Description("Visualiza los fundadores del servidor")]
    public async Task Fundadores(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        DiscordGuild guild = ctx.Guild!;
        DateTime guildCreation = guild.CreationTimestamp.Date;
        DiscordRole noVerificadoRole = guild.Roles[config.Roles.NoVinculado];

        List<DiscordMember> miembrosFundadores = guild.Members.Values
            .Where(x => config.GetFechaEntrada(x.Id, x.JoinedAt).Date == guildCreation && !x.Roles.Contains(noVerificadoRole) && !x.IsBot)
            .OrderBy(x => config.GetFechaEntrada(x.Id, x.JoinedAt))
            .ToList();

        await DiscordInteractivity.PaginarContainerV2Async(
            ctx,
            miembrosFundadores,
            porPagina: 7,
            header: "# Miembros fundadores del servidor\n" +
                    "Personas que entraron al server el día 7 de julio.\n" +
                    "-# Horarios en hora de Argentina (UTC-3).",
            renderItem: miembro =>
            {
                DateTimeOffset entrada = config.GetFechaEntrada(miembro.Id, miembro.JoinedAt).ToOffset(TimeSpan.FromHours(-3));
                return new DiscordSectionComponent(
                    text: $"### {miembro.DisplayName}\n" +
                          $"Entró a las {entrada:HH:mm}",
                    accessory: new DiscordThumbnailComponent(miembro.GuildAvatarUrl ?? miembro.AvatarUrl));
            });
    }

    [Command("Traducir")]
    public async Task Traducir(MessageCommandContext ctx, DiscordMessage targetMessage)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync(true);

        DiscordMember member = await ctx.Guild!.GetMemberAsync(targetMessage.Author!.Id);

        HttpClient client = httpClientFactory.CreateClient();
        string translated = await TranslationHelper.TranslateAsync(client, targetMessage.Content, "auto", "es");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Mensaje Original")
                .WithDescription(targetMessage.Content)
                .WithAuthor(member.DisplayName, null, member.GuildAvatarUrl ?? member.AvatarUrl))
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Traduccion")
                .WithDescription(translated)
                .WithColor(DiscordColor.Green)));
    }
}
