using System.ComponentModel;
using System.Globalization;
using AnilistConEnie.Application.Charts;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Commands.Enums;
using AnilistConEnie.Bot.Commands.Slash.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Entities.Charts;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Extensions.DependencyInjection;
using AnilistConEnie.Bot.Extensions;

namespace AnilistConEnie.Bot.Commands.Slash;

//[TestCommand]
[Command("xp")]
public class Xp(
    BotConfiguration config,
    RangoRoles rangoRoles,
    XpState xpState,
    XpChartService xpChartService,
    IChartRenderer chartRenderer,
    IXpDiarioRepository xpDiarioRepository,
    DiscordBotService discordBotService)
{
    private const string RankThumbnail = "https://media.discordapp.net/attachments/862568630365323264/990747470508204032/unknown.png";

    private ValueTask<DiscordEmoji> UmaPointsAsync(SlashCommandContext ctx) =>
        DiscordEmojiHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.UmaPoints.Get(discordBotService.Debug));

    [Command("top")]
    [Description("Muestra el ranking de experiencia del servidor")]
    public async Task Top(
        SlashCommandContext ctx,
        [Parameter("Categoria")] [Description("Como contabilizar la xp")] TipoXpTopCommand tipo)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<UserXp> xp = xpState.GetGuildXp(ctx.Guild!);

        XpRankingCategory category = tipo switch
        {
            TipoXpTopCommand.Total => XpRankingCategory.Total,
            TipoXpTopCommand.Mensajes => XpRankingCategory.Mensajes,
            TipoXpTopCommand.Intercambios => XpRankingCategory.Intercambios,
            TipoXpTopCommand.Eventos => XpRankingCategory.Eventos,
            TipoXpTopCommand.Challenges => XpRankingCategory.Challenges,
            TipoXpTopCommand.Booster => XpRankingCategory.Booster,
            _ => XpRankingCategory.Otros
        };

        IReadOnlyList<XpRankEntry> rankings = XpRanking.Build(xp, category);

        if (rankings.Count == 0 && tipo is not (TipoXpTopCommand.Total or TipoXpTopCommand.Mensajes))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"No hay registros para la categoria `{((Enum)tipo).GetDescription()}`")
                .WithColor(DiscordColor.Red)));
            return;
        }

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);

        string RivalName(ulong rivalId) => ctx.Guild!.Members[rivalId].DisplayName;

        RankPosition pos = XpRanking.ResolvePosition(rankings, ctx.User.Id);
        string positionDesc = pos.Kind switch
        {
            RankPositionKind.Absent => "No participas en este ranking",
            RankPositionKind.SoloLeader => "Tu posición es #1",
            RankPositionKind.Behind => $"Tu posición es #{pos.PositionNumber} y te faltan {pos.Diff.ToSpanish()} de xp para alcanzar a {RivalName(pos.RivalUserId)}",
            _ => $"Tu posición es #1 y a {RivalName(pos.RivalUserId)} le faltan {pos.Diff.ToSpanish()} de xp para alcanzarte"
        };

        string fullText = string.Join("\n", rankings.Select(rk =>
            $"**#{rk.Rank}** {ctx.Guild!.Members[rk.UserId].DisplayName} - **{rk.Score.ToSpanish()} {umaPoints}**"));

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithColor(DiscordEmojiHelper.GetColor())
            .WithTitle($"Ranking de experiencia [{((Enum)tipo).GetDescription().ToUpper()}]")
            .WithThumbnail(RankThumbnail)
            .WithFooter(positionDesc, ctx.User.AvatarUrl);

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        IEnumerable<Page> pages = InteractivityExtension.GeneratePagesInEmbed(fullText, SplitType.Line, embed);
        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, ephemeral: false, ctx.User, pages);
    }

    [Command("rank")]
    [Description("Muestra el ranking de experiencia de un usuario")]
    public async Task Rank(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario que quieres ver su xp")] DiscordUser? user = null)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        DiscordMember? member = user != null ? ctx.Guild!.Members.GetValueOrDefault(user.Id) : ctx.Member!;
        if (member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"{user!.Mention} no se encuentra en el servidor")
                .WithColor(DiscordColor.Red)));
            return;
        }

        UserXp rank = member.IsBot ? new UserXp() : xpState.GetUserXp(member.Id);

        if (rank.Total <= 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"No se encontro experiencia para el usuario {member.DisplayName}\n\nEs muy probable que sea debido a un error temporal, intenta ejecutar nuevamente el comando")
                .WithColor(DiscordColor.Red)));
            return;
        }

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);
        DiscordRole role = rangoRoles.GetRoleByXp(ctx.Guild!, rank.Total);

        List<UserDailyXp> chartHistory = await xpChartService.GetUserChartHistory(member.Id);
        chartHistory.Add(new UserDailyXp { Xp = rank.Total, Date = RelojServidor.Ahora, UserId = (long)member.Id });

        Dictionary<string, DiscordInteractivity.TabContent> embeds = new();

        #region Resumen
        double promedioXp = chartHistory.GetPromedio();
        XpProgressionInfo prog = XpProgression.Describe(rank.Total, promedioXp);
        RangoEnum nextRango = prog.NextRango;
        long nextRangeXp = prog.NextRangeXp;
        long nextXp = prog.NextXp;
        int mensajesNecesarios = prog.MensajesNecesarios;

        List<UserXp> totalOrdered = xpState.GetGuildXp(ctx.Guild!).OrderByDescending(x => x.Total).ToList();
        int userRank = totalOrdered.FindIndex(x => (ulong)x.UserId == member.Id) + 1;

        string desc = $"### Tienes {rank.Total.ToSpanish()} {umaPoints}\n\n- Tu rango actual es {role.Mention}\n";
        if (nextXp > 0) desc += $"- Obteniendo **{nextXp.ToSpanish()} {umaPoints}** llegarás a **{((Enum)nextRango).GetDescription()}**\n";
        desc += $"- Eres la persona **#{userRank}** con mas xp {umaPoints} del servidor\n";
        desc += $"- Estas obteniendo en promedio **{promedioXp.ToSpanish()}** de xp por día\n";
        if (nextXp > 0) desc += XpProgression.EstimarTiempo(nextXp, promedioXp, ((Enum)nextRango).GetDescription(), mensajesNecesarios);

        byte[] progressBarImage = await chartRenderer.RenderAsync(XpCharts.ProgressBar(rank.Total, RangoXp.MetaProgreso(rank.Total)));
        const string progressBarFile = "xpchartprogress.png";

        embeds.Add("Resumen", new DiscordInteractivity.TabContent(
            new DiscordEmbedBuilder()
                .WithTitle($"Experiencia de {member.DisplayName}")
                .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
                .WithDescription(desc)
                .WithImageUrl($"attachment://{progressBarFile}")
                .Build(),
            progressBarImage,
            progressBarFile));
        #endregion

        #region Detalle (distribución)
        List<PieSlice> valores = [];
        List<(long Valor, string Detalle)> detalles = [];

        foreach (XpCategoryShare share in XpDistribution.Build(rank))
        {
            if (share.Value <= 0) continue;

            (string label, string color, string motivo) = ChartColors.ForXpCategory(share.Category);

            valores.Add(new PieSlice(label, share.Value, color));
            detalles.Add((share.Value, $"- {share.Value.ToSpanish()} {umaPoints} ({share.Percentage}%) fueron obtenidos por {motivo}"));
        }

        byte[] pieImage = await chartRenderer.RenderAsync(XpCharts.Distribution(valores));
        const string pieFile = "xpchartdistribution.png";

        embeds.Add("Detalle", new DiscordInteractivity.TabContent(
            new DiscordEmbedBuilder()
                .WithTitle($"Experiencia de {member.DisplayName}")
                .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
                .WithDescription($"### Total: {rank.Total.ToSpanish()} {umaPoints}\n" + string.Join("\n", detalles.OrderByDescending(x => x.Valor).Select(x => x.Detalle)))
                .WithImageUrl($"attachment://{pieFile}")
                .Build(),
            pieImage,
            pieFile));
        #endregion

        #region Historial
        const int registrosMaximos = 60;
        DateTime hoy = RelojServidor.Hoy;
        int yearActual = hoy.Year;
        DateTime primerDiaUltimoMes = new(hoy.Year, hoy.Month, 1);

        List<UserDailyXp> registrosTrimestrales = chartHistory
            .Where(x => x.Date.Year < yearActual)
            .GroupBy(x => new { x.Date.Year, Trimestre = (x.Date.Month - 1) / 3 + 1 })
            .Select(g => g.OrderBy(x => x.Date).First())
            .OrderBy(x => x.Date)
            .ToList();

        List<UserDailyXp> registrosMesesAnteriores = chartHistory
            .Where(x => x.Date.Year == yearActual && x.Date < primerDiaUltimoMes)
            .GroupBy(x => x.Date.Month)
            .Select(g => g.OrderBy(x => x.Date).First())
            .OrderBy(x => x.Date)
            .ToList();

        int SemanaDelAnio(DateTime fecha) =>
            CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(fecha, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        List<UserDailyXp> semanasUltimoMes = chartHistory
            .Where(x => x.Date >= primerDiaUltimoMes)
            .GroupBy(x => SemanaDelAnio(x.Date))
            .Select(g =>
            {
                if (SemanaDelAnio(hoy) == g.Key)
                {
                    UserDailyXp? regHoy = g.FirstOrDefault(x => x.Date.Date == hoy);
                    if (regHoy != null) return regHoy;
                }
                return g.OrderBy(x => x.Date).First();
            })
            .OrderBy(x => x.Date)
            .ToList();

        List<UserDailyXp> resultado = [.. registrosTrimestrales, .. registrosMesesAnteriores, .. semanasUltimoMes];
        resultado = resultado.OrderBy(x => x.Date).ToList();
        if (resultado.Count > registrosMaximos)
            resultado = resultado.Skip(resultado.Count - registrosMaximos).ToList();

        UserDailyXp minXpValue = resultado.MinBy(x => x.Xp)!;
        RangoEnum prevRango = RangoXp.RangoAnterior(minXpValue.Xp);
        long prevRangeXp = RangoXp.XpRequerida(prevRango);
        long xpSubida = rank.Total - minXpValue.Xp;
        double days = (RelojServidor.Hoy - minXpValue.Date).TotalDays;
        long maxXpChart = (long)(resultado.Max(x => x.Xp) * 1.1);

        string descHistorial =
            $"### Tienes {rank.Total.ToSpanish()} {umaPoints}\n\n- Tu rango actual es {role.Mention}\n" +
            $"- Empezaste teniendo **{minXpValue.Xp.ToSpanish()} {umaPoints}** ({minXpValue.Date.Day}/{minXpValue.Date.Month}/{minXpValue.Date.Year})\n" +
            $"- En {days.ToSpanish()} dias subiste **{xpSubida.ToSpanish()} {umaPoints}**";

        if (nextRango != prevRango && nextRango != RangoXp.RangoAnterior(rank.Total))
            descHistorial += $"\n- El siguiente rango es **{((Enum)nextRango).GetDescription()}** llegando a **{nextRangeXp.ToSpanish()} {umaPoints}**";

        byte[] lineImage = await chartRenderer.RenderAsync(XpCharts.History([.. resultado.Select(x => (x.Xp, x.Date))], prevRangeXp, maxXpChart));
        const string lineFile = "xpcharthistory.png";

        embeds.Add("Historial", new DiscordInteractivity.TabContent(
            new DiscordEmbedBuilder()
                .WithTitle($"Experiencia de {member.DisplayName}")
                .WithDescription(descHistorial)
                .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
                .WithImageUrl($"attachment://{lineFile}")
                .Build(),
            lineImage,
            lineFile));
        #endregion

        await DiscordInteractivity.SwitchTabsAsync(ctx, embeds);
    }

    [Command("top_chart")]
    [Description("Muestra el ranking de experiencia del top en forma de chart")]
    public async Task TopChart(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        List<UserXp> serverRanking = xpState.GetGuildXp(ctx.Guild!).OrderByDescending(x => x.Total).ToList();

        List<string> colors = [.. Enumerable.Range(0, 5).Select(ChartColors.ForIndex)];

        int start = 0;
        UserXp? self = serverRanking.FirstOrDefault(x => x.UserId == (long)ctx.User.Id);
        if (self != null)
        {
            int indexOf = serverRanking.IndexOf(self);
            start = indexOf % 10 <= 5 ? indexOf / 10 * 10 : (indexOf / 10 + 1) * 10;
        }

        while (true)
        {
            List<UserXp> rankings = serverRanking.Skip(start).Take(5).ToList();

            List<(DiscordMember Member, List<UserDailyXp> History)> memberHistories = [];
            foreach (UserXp ranking in rankings)
            {
                if (!ctx.Guild!.Members.TryGetValue((ulong)ranking.UserId, out DiscordMember? member))
                    continue;

                List<UserDailyXp> chartHistory = await xpChartService.GetUserWeeklyHistory(member.Id, ranking.Total);
                memberHistories.Add((member, chartHistory));
            }

            if (memberHistories.Count == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No hay datos de experiencia para mostrar."));
                return;
            }

            long minXpValue = NumberHelper.ObtenerMultiploAnterior(memberHistories.SelectMany(x => x.History).Min(x => x.Xp), 1000);
            long maxXpValue = NumberHelper.ObtenerMultiploSiguiente(memberHistories.SelectMany(x => x.History).Max(x => x.Xp), 1000);

            // La grilla de fechas es la del historial más largo: como todos arrancan la cuenta desde
            // "hoy" en pasos semanales, un historial más corto (usuario más nuevo) es la cola de uno
            // más largo, así que alinearlo "a la derecha" reconstruye su fecha real de ingreso.
            List<UserDailyXp> chartHistoryLabels = memberHistories.MaxBy(x => x.History.Count).History;

            List<LineSeries> series = [];
            int it = 0;
            colors.Shuffle();
            foreach ((DiscordMember member, List<UserDailyXp> history) in memberHistories)
            {
                double[] alignedData = XpTopChartHistory.AlignToGrid(history, chartHistoryLabels.Count);
                series.Add(new LineSeries(member.DisplayName, alignedData, colors[it]));
                it++;
            }

            byte[] image = await chartRenderer.RenderAsync(
                XpCharts.TopMultiLine(series, [.. chartHistoryLabels.Select(x => x.Date)], minXpValue, maxXpValue));
            string fileName = $"{StringHelper.CreateString(10)}.png";

            int end = start + 5;
            DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddActionRowComponent(
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "anterior", "Anterior"),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "siguiente", "Siguiente"))
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Top 10 de experiencia")
                    .WithDescription($"Puestos del {start + 1} al {end}")
                    .WithThumbnail(RankThumbnail)
                    .WithImageUrl($"attachment://{fileName}"))
                .AddFile(fileName, image.ToMemoryStream()));

            InteractivityResult<DSharpPlus.EventArgs.ComponentInteractionCreatedEventArgs> interaction =
                await interactivity.WaitForButtonAsync(message, ctx.User, TimeSpan.FromMinutes(3));

            if (interaction.TimedOut) return;

            await interaction.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            string value = interaction.Result.Id;
            if ((value == "anterior" && start <= 4) || (value == "siguiente" && start >= serverRanking.Count - 4))
                continue;

            start += value == "anterior" ? -5 : 5;
        }
    }

    [Command("top_paises")]
    [Description("Muestra el ranking de experiencia del servidor por pais")]
    public async Task TopPaises(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<UserXp> serverRanking = xpState.GetGuildXp(ctx.Guild!);
        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);

        Dictionary<ulong, CountryXp> xpPerCountry = [];
        foreach (UserXp ranking in serverRanking)
        {
            if (!ctx.Guild!.Members.TryGetValue((ulong)ranking.UserId, out DiscordMember? member)) continue;

            DiscordRole? pais = rangoRoles.GetMemberPais(member);
            if (pais is null) continue;

            long acumulado = xpPerCountry.TryGetValue(pais.Id, out CountryXp prev) ? prev.Xp : 0;
            string nombre = pais.Id == config.Roles.OtrosPais ? "Otros" : pais.Name;
            xpPerCountry[pais.Id] = new CountryXp(pais.Id, nombre, acumulado + ranking.Total);
        }

        List<CountryXp> rankingPaises = xpPerCountry.Values
            .Where(x => x.CountryId != config.Roles.OtrosPais)
            .OrderByDescending(x => x.Xp)
            .ToList();

        IReadOnlyList<CountryXp> res = XpCountryRanking.BuildChartSeries([.. xpPerCountry.Values], config.Roles.OtrosPais);

        List<PieSlice> slices = res
            .Select((x, i) => new PieSlice(x.Name, x.Xp, ChartColors.ForIndex(i)))
            .ToList();

        byte[] image = await chartRenderer.RenderAsync(XpCharts.CountryPie(slices));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Top de experiencia por país")
                .WithDescription(string.Join("\n", rankingPaises.Select(x => $"**{x.Name}**: {x.Xp.ToSpanish()} {umaPoints}")))
                .WithImageUrl("attachment://xpchartcountries.png"))
            .AddFile("xpchartcountries.png", image.ToMemoryStream()));
    }

    [Command("top_cotorreo")]
    [Description("Muestra el ranking de experiencia por dia")]
    public async Task TopCotorreo(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);
        Dictionary<DiscordMember, double> promediosByUser = [];

        foreach (DiscordMember member in ctx.Guild!.Members.Values)
        {
            if (member.IsBot) continue;
            List<UserDailyXp> chartHistory = await xpChartService.GetUserChartHistory(member.Id);
            if (chartHistory.Count == 0) continue;
            promediosByUser.Add(member, chartHistory.GetPromedio());
        }

        int puesto = 0;
        string fullText = string.Join("\n", promediosByUser
            .OrderByDescending(x => x.Value)
            .Take(50)
            .Select(rk => $"- #{++puesto}: **{rk.Key.DisplayName}** {rk.Value.ToSpanish()} {umaPoints}"));

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithTitle("Top de cotorreo")
            .WithFooter("El cotorreo es el promedio de xp ganado diariamente del usuario")
            .WithColor(DiscordColor.Blurple);

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        IEnumerable<Page> pages = InteractivityExtension.GeneratePagesInEmbed(fullText, SplitType.Line, embed);
        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, ephemeral: false, ctx.User, pages);
    }

    [Command("top_parcial")]
    [Description("Muestra el ranking de experiencia ganada en el período en curso")]
    public async Task TopPorFecha(
        SlashCommandContext ctx,
        [Parameter("Rango")] [Description("Período calendario a contabilizar")] TipoXpTopParcialCommand rango)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        XpRangoParcial rangoParcial = rango switch
        {
            TipoXpTopParcialCommand.Anual => XpRangoParcial.Anual,
            TipoXpTopParcialCommand.Mensual => XpRangoParcial.Mensual,
            TipoXpTopParcialCommand.Semanal => XpRangoParcial.Semanal,
            _ => XpRangoParcial.Diario
        };

        DateOnly inicio = XpRankingParcial.InicioRango(rangoParcial, DateOnly.FromDateTime(RelojServidor.Hoy));

        List<UserXp> xp = xpState.GetGuildXp(ctx.Guild!);
        Dictionary<long, long> baselines = (await xpDiarioRepository.ObtenerBaseline(inicio))
            .ToDictionary(x => x.UserId, x => x.Xp);

        IReadOnlyList<XpRankEntry> rankings = XpRankingParcial.Build(xp, baselines);

        if (rankings.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription($"Todavía no hay experiencia ganada en el rango `{((Enum)rango).GetDescription()}`")
                .WithColor(DiscordColor.Red)));
            return;
        }

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);

        string RivalName(ulong rivalId) => ctx.Guild!.Members[rivalId].DisplayName;

        RankPosition pos = XpRanking.ResolvePosition(rankings, ctx.User.Id);
        string positionDesc = pos.Kind switch
        {
            RankPositionKind.Absent => "No participas en este ranking",
            RankPositionKind.SoloLeader => "Tu posición es #1",
            RankPositionKind.Behind => $"Tu posición es #{pos.PositionNumber} y te faltan {pos.Diff.ToSpanish()} de xp para alcanzar a {RivalName(pos.RivalUserId)}",
            _ => $"Tu posición es #1 y a {RivalName(pos.RivalUserId)} le faltan {pos.Diff.ToSpanish()} de xp para alcanzarte"
        };

        string fullText = string.Join("\n", rankings.Select(rk =>
            $"**#{rk.Rank}** {ctx.Guild!.Members[rk.UserId].DisplayName} - **{rk.Score.ToSpanish()} {umaPoints}**"));

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithColor(DiscordEmojiHelper.GetColor())
            .WithTitle($"Ranking de experiencia [{((Enum)rango).GetDescription().ToUpper()}]")
            .WithThumbnail(RankThumbnail)
            .WithFooter(positionDesc, ctx.User.AvatarUrl);

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        IEnumerable<Page> pages = InteractivityExtension.GeneratePagesInEmbed(fullText, SplitType.Line, embed);
        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, ephemeral: false, ctx.User, pages);
    }

    [Command("listaproxrango")]
    [Description("Muestra las personas que estan por subir de rango")]
    public async Task ProxPorSubirRango(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);
        List<UserXp> xp = xpState.GetGuildXp(ctx.Guild!);
        List<(DiscordMember Member, long NextXp, RangoEnum NextRango)> ret = [];

        foreach (UserXp member in xp)
        {
            RangoEnum nextRango = RangoXp.RangoSiguiente(member.Total);
            long nextXp = RangoXp.XpRequerida(nextRango) - member.Total;

            if (nextXp <= 0 || !ctx.Guild!.Members.TryGetValue((ulong)member.UserId, out DiscordMember? user)) continue;
            if (user.Roles.Any(x => x.Id == config.Roles.Inactivo)) continue;

            if (XpProgression.EstaProximoASubir(nextRango, nextXp)) ret.Add((user, nextXp, nextRango));
        }

        if (ret.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Sin resultados")
                .WithDescription("No hay nadie que esté por subir de rango en este momento")
                .WithColor(DiscordColor.Yellow)));
            return;
        }

        string desc = string.Join("\n", ret
            .OrderBy(x => x.NextXp)
            .Select(x => $"- **{x.Member.DisplayName}** le faltan **{x.NextXp}** {umaPoints} para llegar a **{((Enum)x.NextRango).GetDescription()}**"));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithTitle("Usuarios que estan por subir de rango")
            .WithDescription(desc)
            .WithColor(DiscordColor.Green)));
    }

    [Command("beneficios")]
    [Description("Muestra tus beneficios desbloqueados en el servidor")]
    public async Task Beneficios(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        DiscordMember member = ctx.Member!;
        DiscordEmoji tenshiEmote = DiscordEmoji.FromGuildEmote(ctx.Client, config.Emotes.Tenshi);
        string desc = string.Empty;

        foreach (RangoEnum rango in Enum.GetValues<RangoEnum>())
        {
            string? items = RangoBeneficios.Items(rango, ctx.Guild!, config);
            if (items == null || !rangoRoles.RangoAPartirDe(ctx.Guild!, member, rango, false)) continue;

            desc += $"### {RangoBeneficios.Emoji(rango)} {((Enum)rango).GetDescription()}:\n{items}\n\n";
        }

        if (member.PremiumSince != null)
            desc += $"### 😇 Tenshi:\n- Emotes exclusivos `{tenshiEmote}`\n- 1 a 3 de XP extra por mensaje\n\n";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithTitle("Beneficios desbloqueados")
            .WithDescription(string.IsNullOrEmpty(desc) ? "Sin beneficios desbloqueados" : desc)
            .WithAuthor(member.DisplayName, iconUrl: member.AvatarUrl)
            .WithColor(DiscordColor.Blurple)));
    }
}
