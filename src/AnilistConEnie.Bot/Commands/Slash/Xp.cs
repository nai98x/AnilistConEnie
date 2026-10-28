using System.ComponentModel;
using System.Globalization;
using AnilistConEnie.Application.Charts;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Commands.Framework.Choices;
using AnilistConEnie.Bot.Commands.Framework.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Entities.Charts;
using AnilistConEnie.Domain.Enum;
using AnilistConEnie.Domain.Interfaces;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using AnilistConEnie.Bot.Extensions;

namespace AnilistConEnie.Bot.Commands.Slash;

//[TestCommand]
[Command("xp")]
public class Xp(
    BotConfiguration config,
    RangoRoles rangoRoles,
    XpChartService xpChartService,
    IChartRenderer chartRenderer,
    IXpDiarioRepository xpDiarioRepository,
    IXpUsuariosRepository xpUsuariosRepository,
    DiscordBotService discordBotService,
    InteractivityExtension interactivity)
{
    private const string RankThumbnail = "https://media.discordapp.net/attachments/862568630365323264/990747470508204032/unknown.png";

    private ValueTask<DiscordEmoji> UmaPointsAsync(SlashCommandContext ctx) =>
        DiscordEmojiHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.Bot.UmaPoints);

    [Command("top")]
    [Description("Muestra el ranking de experiencia del servidor")]
    public async Task Top(
        SlashCommandContext ctx,
        [Parameter("Categoria")] [Description("Como contabilizar la xp")] TipoXpTopCommand tipo)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<UserXp> xp = await xpUsuariosRepository.ObtenerRankingDelGuild(ctx.Guild!);

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
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"No hay registros para la categoria `{((Enum)tipo).GetDescription()}`")));
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

        DateTime hoy = RelojServidor.Hoy;
        Dictionary<ulong, double> promedios = tipo == TipoXpTopCommand.Total
            ? (await xpDiarioRepository.ObtenerExtremos()).ToDictionary(x => (ulong)x.UserId, x => x.GetPromedio(hoy))
            : [];

        await DiscordInteractivity.PaginarContainerV2Async(
            ctx,
            rankings,
            porPagina: 5,
            header: $"## Ranking de experiencia [{((Enum)tipo).GetDescription().ToUpper()}]\n-# {positionDesc}",
            renderItem: rk =>
            {
                DiscordMember miembro = ctx.Guild!.Members[rk.UserId];
                return new DiscordSectionComponent(
                    text: $"### #{rk.Rank} {miembro.DisplayName}\n" +
                          $"{rk.Score.ToSpanish()} {umaPoints}" +
                          (promedios.TryGetValue(rk.UserId, out double promedio) ? $"\n*Promedio diario: {promedio.ToSpanish()}*" : ""),
                    accessory: new DiscordThumbnailComponent(miembro.AvatarUrlPreferido()));
            },
            color: DiscordEmojiHelper.GetColor(),
            separarItems: true,
            indiceInicial: Math.Max(0, rankings.ToList().FindIndex(rk => rk.UserId == ctx.User.Id)),
            headerThumbnail: RankThumbnail);
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
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"{user!.Mention} no se encuentra en el servidor")));
            return;
        }

        List<UserXp> totalOrdered = (await xpUsuariosRepository.ObtenerRankingDelGuild(ctx.Guild!)).OrderByDescending(x => x.Total).ToList();
        UserXp rank = member.IsBot ? new UserXp() : totalOrdered.FirstOrDefault(x => (ulong)x.UserId == member.Id) ?? new UserXp();

        if (rank.Total <= 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"No se encontro experiencia para el usuario {member.DisplayName}\n\nEs muy probable que sea debido a un error temporal, intenta ejecutar nuevamente el comando")));
            return;
        }

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);
        DiscordRole role = rangoRoles.GetRoleByXp(ctx.Guild!, rank.Total);

        List<UserDailyXp> chartHistory = await xpChartService.GetUserChartHistory(member.Id);
        chartHistory.Add(new UserDailyXp { Xp = rank.Total, Date = RelojServidor.Ahora, UserId = (long)member.Id });

        Dictionary<string, DiscordInteractivity.TabContent> embeds = new();

        #region Resumen
        double promedioXp = chartHistory.GetPromedio();
        XpProgressionInfo prog = XpProgression.Describe(rank.Total);
        RangoEnum nextRango = prog.NextRango;
        long nextRangeXp = prog.NextRangeXp;
        long nextXp = prog.NextXp;
        int mensajesNecesarios = prog.MensajesNecesarios;

        int userRank = totalOrdered.FindIndex(x => (ulong)x.UserId == member.Id) + 1;

        bool esPropio = member.Id == ctx.User.Id;
        string tiene = esPropio ? "Tienes" : "Tiene";
        string rangoActual = esPropio ? "Tu rango actual es" : "Su rango actual es";

        string desc = $"### {tiene} {rank.Total.ToSpanish()} {umaPoints}\n\n- {rangoActual} {role.Mention}\n";
        if (nextXp > 0) desc += $"- Obteniendo **{nextXp.ToSpanish()} {umaPoints}** {(esPropio ? "llegarás" : "llegará")} a **{((Enum)nextRango).GetDescription()}**\n";
        desc += $"- {(esPropio ? "Eres" : "Es")} la persona **#{userRank}** con mas xp {umaPoints} del servidor\n";
        desc += $"- {(esPropio ? "Estás" : "Está")} obteniendo en promedio **{promedioXp.ToSpanish()}** de xp por día\n";
        if (nextXp > 0) desc += XpProgression.EstimarTiempo(nextXp, promedioXp, ((Enum)nextRango).GetDescription(), mensajesNecesarios, esPropio);

        DiscordEmbedBuilder resumenEmbed = new DiscordEmbedBuilder()
            .WithTitle($"Experiencia de {member.DisplayName}")
            .WithThumbnail(member.AvatarUrlPreferido())
            .WithDescription(desc);

        if (nextXp > 0)
        {
            byte[] progressBarImage = await chartRenderer.RenderAsync(XpCharts.ProgressBar(rank.Total, nextRangeXp));
            const string progressBarFile = "xpchartprogress.png";

            embeds.Add("Resumen", new DiscordInteractivity.TabContent(
                resumenEmbed.WithImageUrl($"attachment://{progressBarFile}").Build(),
                progressBarImage,
                progressBarFile));
        }
        else
        {
            embeds.Add("Resumen", new DiscordInteractivity.TabContent(resumenEmbed.Build()));
        }
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
                .WithThumbnail(member.AvatarUrlPreferido())
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
            $"### {tiene} {rank.Total.ToSpanish()} {umaPoints}\n\n- {rangoActual} {role.Mention}\n" +
            $"- {(esPropio ? "Empezaste" : "Empezó")} teniendo **{minXpValue.Xp.ToSpanish()} {umaPoints}** ({minXpValue.Date.Day}/{minXpValue.Date.Month}/{minXpValue.Date.Year})\n" +
            $"- En {days.ToSpanish()} dias {(esPropio ? "subiste" : "subió")} **{xpSubida.ToSpanish()} {umaPoints}**";

        if (nextRango != prevRango && nextRango != RangoXp.RangoAnterior(rank.Total))
            descHistorial += $"\n- El siguiente rango es **{((Enum)nextRango).GetDescription()}** llegando a **{nextRangeXp.ToSpanish()} {umaPoints}**";

        byte[] lineImage = await chartRenderer.RenderAsync(XpCharts.History([.. resultado.Select(x => (x.Xp, x.Date))], prevRangeXp, maxXpChart));
        const string lineFile = "xpcharthistory.png";

        embeds.Add("Historial", new DiscordInteractivity.TabContent(
            new DiscordEmbedBuilder()
                .WithTitle($"Experiencia de {member.DisplayName}")
                .WithDescription(descHistorial)
                .WithThumbnail(member.AvatarUrlPreferido())
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

        List<UserXp> serverRanking = (await xpUsuariosRepository.ObtenerRankingDelGuild(ctx.Guild!)).OrderByDescending(x => x.Total).ToList();

        List<string> colors = [.. Enumerable.Range(0, 5).Select(ChartColors.ForIndex)];

        const int porPagina = 5;
        int start = 0;
        int indexOf = serverRanking.FindIndex(x => x.UserId == (long)ctx.User.Id);
        if (indexOf >= 0)
            start = indexOf / porPagina * porPagina;

        XpTopChartPeriodo periodo = XpTopChartPeriodo.Anual;

        while (true)
        {
            List<UserXp> rankings = serverRanking.Skip(start).Take(porPagina).ToList();

            List<(DiscordMember Member, List<UserDailyXp> History)> memberHistories = [];
            foreach (UserXp ranking in rankings)
            {
                if (!ctx.Guild!.Members.TryGetValue((ulong)ranking.UserId, out DiscordMember? member))
                    continue;

                List<UserDailyXp> chartHistory = await xpChartService.GetUserTopChartHistory(member.Id, ranking.Total, periodo);
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
            // "hoy" en pasos fijos, un historial más corto (usuario más nuevo) es la cola de uno
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

            int end = Math.Min(start + porPagina, serverRanking.Count);
            DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddActionRowComponent(
                    BotonPeriodo(XpTopChartPeriodo.Anual, "Anual", periodo),
                    BotonPeriodo(XpTopChartPeriodo.Mensual, "Mensual", periodo),
                    BotonPeriodo(XpTopChartPeriodo.Semanal, "Semanal", periodo))
                .AddActionRowComponent(
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "anterior", "Anterior"),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "siguiente", "Siguiente"))
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Top de experiencia")
                    .WithDescription($"Puestos del {start + 1} al {end} · {DescripcionPeriodo(periodo)}")
                    .WithThumbnail(RankThumbnail)
                    .WithImageUrl($"attachment://{fileName}"))
                .AddFile(fileName, image.ToMemoryStream()));

            InteractivityResult<DSharpPlus.EventArgs.ComponentInteractionCreatedEventArgs> interaction =
                await interactivity.WaitForButtonAsync(message, ctx.User, TimeSpan.FromMinutes(3));

            if (interaction.TimedOut) return;

            await interaction.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            string value = interaction.Result.Id;
            if (Enum.TryParse(value, out XpTopChartPeriodo nuevoPeriodo))
            {
                periodo = nuevoPeriodo;
                continue;
            }

            if ((value == "anterior" && start == 0) || (value == "siguiente" && start + porPagina >= serverRanking.Count))
                continue;

            start += value == "anterior" ? -porPagina : porPagina;
        }
    }

    private static DiscordButtonComponent BotonPeriodo(XpTopChartPeriodo valor, string label, XpTopChartPeriodo actual) =>
        new(DiscordButtonStyle.Primary, valor.ToString(), label, valor == actual);

    private static string DescripcionPeriodo(XpTopChartPeriodo periodo) => periodo switch
    {
        XpTopChartPeriodo.Mensual => "Último mes",
        XpTopChartPeriodo.Semanal => "Última semana",
        _ => "Último año"
    };

    [Command("top_paises")]
    [Description("Muestra el ranking de experiencia del servidor por pais")]
    public async Task TopPaises(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<UserXp> serverRanking = await xpUsuariosRepository.ObtenerRankingDelGuild(ctx.Guild!);
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

        if (xpPerCountry.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("No hay miembros con rol de país y experiencia registrada.")));
            return;
        }

        List<CountryXp> rankingPaises = xpPerCountry.Values
            .Where(x => x.CountryId != config.Roles.OtrosPais)
            .OrderByDescending(x => x.Xp)
            .ToList();

        IReadOnlyList<CountryXp> res = XpCountryRanking.BuildChartSeries([.. xpPerCountry.Values], config.Roles.OtrosPais, ChartColors.MaxPorcionesTorta);
        IReadOnlyList<PieSlice> slices = XpCharts.CountrySlices(res, config.Roles.OtrosPais);

        byte[] image = await chartRenderer.RenderAsync(XpCharts.CountryPie(slices));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Top de experiencia por país")
                .WithDescription(string.Join("\n", rankingPaises.Select(x => $"**{x.Name}**: {x.Xp.ToSpanish()} {umaPoints}")))
                .WithImageUrl("attachment://xpchartcountries.png"))
            .AddFile("xpchartcountries.png", image.ToMemoryStream()));
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

        DateOnly hoy = DateOnly.FromDateTime(RelojServidor.Hoy);
        DateOnly inicio = XpRankingParcial.InicioRango(rangoParcial, hoy);

        List<UserXp> xp = await xpUsuariosRepository.ObtenerRankingDelGuild(ctx.Guild!);
        Dictionary<long, long> baselines = (await xpDiarioRepository.ObtenerBaseline(inicio))
            .ToDictionary(x => x.UserId, x => x.Xp);

        IReadOnlyList<XpRankEntry> rankings = XpRankingParcial.Build(xp, baselines);

        if (rankings.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"Todavía no hay experiencia ganada en el rango `{((Enum)rango).GetDescription()}`")));
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

        bool mostrarPromedio = rangoParcial != XpRangoParcial.Diario;

        await DiscordInteractivity.PaginarContainerV2Async(
            ctx,
            rankings,
            porPagina: 5,
            header: $"## Ranking de experiencia [{((Enum)rango).GetDescription().ToUpper()}]\n-# {positionDesc}",
            renderItem: rk =>
            {
                DiscordMember miembro = ctx.Guild!.Members[rk.UserId];
                return new DiscordSectionComponent(
                    text: $"### #{rk.Rank} {miembro.DisplayName}\n" +
                          $"{rk.Score.ToSpanish()} {umaPoints}" +
                          (mostrarPromedio ? $"\n*Promedio diario: {XpRankingParcial.PromedioDiario(rk.Score, inicio, hoy).ToSpanish()}*" : ""),
                    accessory: new DiscordThumbnailComponent(miembro.AvatarUrlPreferido()));
            },
            color: DiscordEmojiHelper.GetColor(),
            separarItems: true,
            indiceInicial: Math.Max(0, rankings.ToList().FindIndex(rk => rk.UserId == ctx.User.Id)),
            headerThumbnail: RankThumbnail);
    }

    [Command("listaproxrango")]
    [Description("Muestra las personas que estan por subir de rango")]
    public async Task ProxPorSubirRango(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);
        List<UserXp> xp = await xpUsuariosRepository.ObtenerRankingDelGuild(ctx.Guild!);
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

        List<(DiscordMember Member, long NextXp, RangoEnum NextRango)> ordenados = [.. ret.OrderBy(x => x.NextXp).Take(10)];

        await DiscordInteractivity.PaginarContainerV2Async(
            ctx,
            ordenados,
            porPagina: 10,
            header: "## Usuarios que están por subir de rango",
            renderItem: x => new DiscordSectionComponent(
                text: $"### {x.Member.DisplayName}\n" +
                      $"Le faltan **{x.NextXp.ToSpanish()}** {umaPoints} para llegar a **{((Enum)x.NextRango).GetDescription()}**",
                accessory: new DiscordThumbnailComponent(x.Member.AvatarUrlPreferido())),
            color: DiscordColor.Green,
            headerThumbnail: RankThumbnail);
    }

    [Command("beneficios")]
    [Description("Muestra tus beneficios desbloqueados en el servidor")]
    public async Task Beneficios(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        DiscordMember member = ctx.Member!;
        DiscordEmoji tenshiEmote = DiscordEmoji.FromGuildEmote(ctx.Client, config.Emotes.Guild.Tenshi);
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
