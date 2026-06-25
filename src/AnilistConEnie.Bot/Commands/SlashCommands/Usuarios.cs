using System.ComponentModel;
using System.Globalization;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Commands.Enums;
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
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

//[TestCommand]
public class Usuarios(
    IUsuariosDiscordRepository usuariosDiscordRepository,
    XpState xpState,
    RangoRoles rangoRoles,
    BotConfiguration config,
    DiscordLogService logService,
    IHttpClientFactory httpClientFactory,
    DiscordBotService discordBotService)
{
    [Command("birthdays")]
    [Description("Muestra los cumpleaños de los usuarios")]
    public async Task Birthdays(
        SlashCommandContext ctx,
        [Parameter("Filtro")] [Description("Si quieres ver los cumpleaños del mes o todos los registrados")] FiltroCumple filtro)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        bool month = filtro == FiltroCumple.SoloDelMes;

        DiscordGuild guild = ctx.Guild!;
        CultureInfo es = CultureInfo.CreateSpecificCulture("es");

        List<UserCumple> lista = await usuariosDiscordRepository.GetBirthdays(month);
        List<UserCumple> hoy = await usuariosDiscordRepository.GetBirthdaysHoy();

        List<(UserCumple Cumple, DiscordMember Miembro)> proximos = lista
            .OrderBy(x => x.BirthdayActual)
            .Select(x => (Cumple: x, Miembro: guild.Members.GetValueOrDefault((ulong)x.Id)))
            .Where(x => x.Miembro is not null)
            .Select(x => (x.Cumple, x.Miembro!))
            .ToList();

        string header = "# Cumpleaños\n";

        List<string> festejanHoy = hoy
            .Where(u => guild.Members.ContainsKey((ulong)u.Id))
            .Select(u => guild.Members[(ulong)u.Id].DisplayName)
            .ToList();

        if (festejanHoy.Count > 0)
        {
            header += "**Cumplen años hoy:**\n" +
                      string.Join("\n", festejanHoy.Select(nombre => $"- **{nombre}**")) + "\n\n";
        }

        header += month ? "**Cumplen años en el próximo mes:**" : "**Cumplen años próximamente:**";

        if (proximos.Count == 0)
            header += "\n-# (No hay ningún usuario registrado que cumpla años este mes)";

        await DiscordInteractivity.PaginarContainerV2Async(
            ctx,
            proximos,
            porPagina: 8,
            header: header,
            renderItem: x =>
            {
                string dia = x.Cumple.FechaOriginal.ToString("dddd", es);
                string mes = x.Cumple.FechaOriginal.ToString("MMMM", es);
                return new DiscordSectionComponent(
                    text: $"### {x.Miembro.DisplayName}\n" +
                          $"Cumple el {dia} {x.Cumple.FechaOriginal.Day} de {mes}",
                    accessory: new DiscordThumbnailComponent(x.Miembro.GuildAvatarUrl ?? x.Miembro.AvatarUrl));
            },
            separarItems: true);
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

        DiscordEmoji umaPoints = await DiscordEmojiHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.UmaPoints.Get(discordBotService.Debug));

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
                long xp = xpState.GetUserXp(miembro.Id).Total;
                DiscordRole rango = rangoRoles.GetRoleByXp(guild, xp);
                return new DiscordSectionComponent(
                    text: $"### {miembro.DisplayName}\n" +
                          $"Entró a las {entrada:HH:mm}\n" +
                          $"{xp.ToSpanish()} {umaPoints} - {rango.Mention}",
                    accessory: new DiscordThumbnailComponent(miembro.GuildAvatarUrl ?? miembro.AvatarUrl));
            },
            separarItems: true);
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
