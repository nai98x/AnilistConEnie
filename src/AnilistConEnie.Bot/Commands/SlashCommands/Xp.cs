using System.ComponentModel;
using System.Globalization;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Commands.Enums;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

//[TestCommand]
public class Xp(
    BotConfiguration config,
    DiscordHelper discordHelper,
    XpState xpState,
    IChartClient chartClient,
    DiscordBotService discordBotService)
{
    private const string RankThumbnail = "https://media.discordapp.net/attachments/862568630365323264/990747470508204032/unknown.png";
    private const ulong OtrosPaisRoleId = 1072636983643480127;
    private const ulong TeiouChannelId = 1263346364750758019;
    private const ulong TenshiEmoteId = 1236843853882069103;

    private ValueTask<DiscordEmoji> UmaPointsAsync(SlashCommandContext ctx) =>
        DiscordHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.UmaPoints.Get(discordBotService.Debug));

    [Command("top")]
    [Description("Muestra el ranking de experiencia del servidor")]
    public async Task Top(
        SlashCommandContext ctx,
        [Parameter("Categoria")] [Description("Como contabilizar la xp")] TipoXpTopCommand tipo)
    {
        await ctx.DeferResponseAsync();

        DiscordEmoji ghost = DiscordEmoji.FromUnicode("👻");
        List<UserXp> xp = xpState.GetGuildXp(ctx.Guild!);
        List<RankEntry> rankings = [];

        switch (tipo)
        {
            case TipoXpTopCommand.Total:
                int i = 0;
                foreach (UserXp xpUsr in xp.OrderByDescending(x => x.Total))
                    rankings.Add(new RankEntry(++i, xpUsr.Total, (ulong)xpUsr.UserId));
                break;
            case TipoXpTopCommand.Mensajes:
                int y = 0;
                IEnumerable<(long UserId, long Mensajes)> mensajes = xp
                    .Select(x => (x.UserId, x.Total - x.Booster - x.Intercambios - x.Challenges - x.Eventos - x.Otros))
                    .OrderByDescending(x => x.Item2);
                foreach ((long userId, long score) in mensajes)
                    if (score > 0) rankings.Add(new RankEntry(++y, score, (ulong)userId));
                break;
            default:
                int z = 0;
                IEnumerable<(long UserId, long Score)> categoria = tipo switch
                {
                    TipoXpTopCommand.Eventos => xp.Select(x => (x.UserId, x.Eventos)),
                    TipoXpTopCommand.Challenges => xp.Select(x => (x.UserId, x.Challenges)),
                    TipoXpTopCommand.Intercambios => xp.Select(x => (x.UserId, x.Intercambios)),
                    TipoXpTopCommand.Booster => xp.Select(x => (x.UserId, x.Booster)),
                    _ => xp.Select(x => (x.UserId, x.Otros))
                };
                foreach ((long userId, long score) in categoria.OrderByDescending(x => x.Item2))
                    if (score > 0) rankings.Add(new RankEntry(++z, score, (ulong)userId));

                if (rankings.Count == 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription($"No hay registros para la categoria `{((Enum)tipo).GetDescription()}`")
                        .WithColor(DiscordColor.Red)));
                    return;
                }
                break;
        }

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);

        string positionDesc = "No participas en este ranking";
        int position = rankings.FindIndex(x => x.UserId == ctx.User.Id);
        if (position >= 0)
        {
            RankEntry you = rankings[position];
            if (rankings.Count == 1)
            {
                positionDesc = "Tu posición es #1";
            }
            else if (position > 0)
            {
                RankEntry rival = rankings[position - 1];
                string rivalName = ctx.Guild!.Members.TryGetValue(rival.UserId, out DiscordMember? r) ? r.DisplayName : "un fantasma";
                positionDesc = $"Tu posición es #{position + 1} y te faltan {(rival.Score - you.Score).ToSpanish()} de xp para alcanzar a {rivalName}";
            }
            else
            {
                RankEntry rival = rankings[1];
                string rivalName = ctx.Guild!.Members.TryGetValue(rival.UserId, out DiscordMember? r) ? r.DisplayName : "un fantasma";
                positionDesc = $"Tu posición es #1 y a {rivalName} le faltan {(you.Score - rival.Score).ToSpanish()} de xp para alcanzarte";
            }
        }

        string fullText = string.Join("\n", rankings.Select(rk =>
        {
            string name = ctx.Guild!.Members.TryGetValue(rk.UserId, out DiscordMember? member) ? member.DisplayName : ghost.ToString();
            return $"**#{rk.Rank}** {name} - **{rk.Score.ToSpanish()} {umaPoints}**";
        }));

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithColor(DiscordHelper.GetColor())
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
        await ctx.DeferResponseAsync();

        DiscordMember member = user != null ? ctx.Guild!.Members[user.Id] : ctx.Member!;
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
        DiscordRole role = discordHelper.GetRoleByXp(ctx.Guild!, rank.Total);

        List<UserDailyXp> chartHistory = await xpState.GetUserChartHistory(member.Id);
        chartHistory.Add(new UserDailyXp { Xp = rank.Total, Date = DateTime.Now, UserId = (long)member.Id });

        Dictionary<string, DiscordEmbed> embeds = new();

        #region Resumen
        RangoEnum nextRango = DiscordHelper.GetNextRangoByXp(rank.Total);
        long nextRangeXp = DiscordHelper.GetXpFromRango(nextRango);
        long nextXp = nextRangeXp - rank.Total;
        double promedioXp = chartHistory.GetPromedio();

        int mensajesNecesarios = Math.Max(0, (int)Math.Ceiling((nextXp - promedioXp) / 15.0));

        string progressBarConfig = $@"{{
                      ""type"": ""horizontalBar"",
                      ""data"": {{
                        ""datasets"": [
                          {{
                            ""label"": ""Dataset 1"",
                            ""data"": [ {rank.Total} ],
                            ""backgroundColor"": ""rgba(54, 162, 235, 0.6)"",
                            ""fill"": false,
                            ""type"": ""horizontalBar"",
                            ""borderColor"": ""rgba(54, 162, 235, 0.6)"",
                            ""borderWidth"": 3
                          }}
                        ],
                        ""labels"": [ """" ]
                      }},
                      ""options"": {{
                        ""legend"": {{ ""display"": false }},
                        ""scales"": {{
                          ""xAxes"": [
                            {{
                              ""ticks"": {{
                                ""beginAtZero"": true,
                                ""fontSize"": 17,
                                ""fontColor"": ""#ffffff"",
                                ""fontStyle"": ""bold"",
                                ""min"": 0,
                                ""max"": {nextRangeXp},
                                ""stepSize"": {nextRangeXp}
                              }},
                              ""gridLines"": {{ ""color"": ""rgba(255, 255, 255, 1)"", ""zeroLineColor"": ""rgba(255, 255, 255, 1)"" }}
                            }}
                          ],
                          ""yAxes"": []
                        }},
                        ""plugins"": {{
                          ""datalabels"": {{
                            ""display"": true,
                            ""align"": ""center"",
                            ""anchor"": ""center"",
                            ""backgroundColor"": ""#ffffff"",
                            ""borderColor"": ""#ddd"",
                            ""borderRadius"": 6,
                            ""borderWidth"": 1,
                            ""padding"": 5,
                            ""color"": ""#000000"",
                            ""font"": {{ ""size"": 25 }}
                          }}
                        }}
                      }}
                    }}";

        List<UserXp> totalOrdered = xpState.GetGuildXp(ctx.Guild!).OrderByDescending(x => x.Total).ToList();
        int userRank = totalOrdered.FindIndex(x => (ulong)x.UserId == member.Id) + 1;

        string desc = $"### Tienes {rank.Total.ToSpanish()} {umaPoints}\n\n- Tu rango actual es {role.Mention}\n";
        if (nextXp > 0) desc += $"- Obteniendo **{nextXp.ToSpanish()} {umaPoints}** llegarás a **{((Enum)nextRango).GetDescription()}**\n";
        desc += $"- Eres la persona **#{userRank}** con mas xp {umaPoints} del servidor\n";
        desc += $"- Estas obteniendo en promedio **{promedioXp.ToSpanish()}** de xp por día\n";
        if (nextXp > 0) desc += DiscordHelper.EstimarTiempoEstimadoRango(nextXp, promedioXp, ((Enum)nextRango).GetDescription(), mensajesNecesarios);

        string progressBarUrl = await chartClient.CreateUrlAsync(new ChartRequest { Config = progressBarConfig, Width = 500, Height = 150 });

        embeds.Add("Resumen", new DiscordEmbedBuilder()
            .WithTitle($"Experiencia de {member.DisplayName}")
            .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
            .WithDescription(desc)
            .WithImageUrl(progressBarUrl)
            .Build());
        #endregion

        #region Detalle (distribución)
        long challengesXp = rank.Challenges;
        long eventosXp = rank.Eventos;
        long intercambiosXp = rank.Intercambios;
        long otrosXp = rank.Otros;

        decimal challengesPercentage = Math.Round(Convert.ToDecimal(challengesXp * 100 / rank.Total), 2);
        decimal eventosPercentage = Math.Round(Convert.ToDecimal(eventosXp * 100 / rank.Total), 2);
        decimal intercambiosPercentage = Math.Round(Convert.ToDecimal(intercambiosXp * 100 / rank.Total), 2);
        decimal otrosPercentage = Math.Round(Convert.ToDecimal(otrosXp * 100 / rank.Total), 2);

        long messagesXp = rank.Total - challengesXp - eventosXp - intercambiosXp - otrosXp;
        decimal messagesXpPercentage = 100 - challengesPercentage - eventosPercentage - intercambiosPercentage - otrosPercentage;

        List<(long Valor, string Label, string Color)> valores = [];
        List<(long Valor, string Detalle)> detalles = [];

        if (messagesXp > 0)
        {
            valores.Add((messagesXp, "Mensajes", "#FF6384"));
            detalles.Add((messagesXp, $"- {messagesXp.ToSpanish()} {umaPoints} ({messagesXpPercentage}%) fueron obtenidos por mensajes"));
        }
        if (challengesXp > 0)
        {
            valores.Add((challengesXp, "Challenges", "#36A2EB"));
            detalles.Add((challengesXp, $"- {challengesXp.ToSpanish()} {umaPoints} ({challengesPercentage}%) fueron obtenidos por challenges"));
        }
        if (eventosXp > 0)
        {
            valores.Add((eventosXp, "Eventos y actividades", "#23C46C"));
            detalles.Add((eventosXp, $"- {eventosXp.ToSpanish()} {umaPoints} ({eventosPercentage}%) fueron obtenidos por eventos y actividades"));
        }
        if (intercambiosXp > 0)
        {
            valores.Add((intercambiosXp, "Intercambios", "#C4BA23"));
            detalles.Add((intercambiosXp, $"- {intercambiosXp.ToSpanish()} {umaPoints} ({intercambiosPercentage}%) fueron obtenidos por intercambios"));
        }
        if (otrosXp > 0)
        {
            valores.Add((otrosXp, "Otros", "#8C23C4"));
            detalles.Add((otrosXp, $"- {otrosXp.ToSpanish()} {umaPoints} ({otrosPercentage}%) fueron obtenidos por otros motivos"));
        }

        string pieConfig = $@"{{
                      ""type"": ""pie"",
                      ""data"": {{
                        ""datasets"": [
                          {{
                            ""data"": [ {string.Join(",", valores.Select(x => $"{x.Valor}"))} ],
                            ""backgroundColor"": [ {string.Join(",", valores.Select(x => $"\"{x.Color}\""))} ],
                            ""label"": ""Dataset 1"",
                            ""type"": ""pie"",
                            ""borderColor"": [ {string.Join(",", valores.Select(x => $"\"{x.Color}\""))} ],
                            ""borderWidth"": 3
                          }}
                        ],
                        ""labels"": [ {string.Join(",", valores.Select(x => $"\"{x.Label}\""))} ]
                      }},
                      ""options"": {{
                        ""legend"": {{
                          ""display"": true,
                          ""position"": ""top"",
                          ""labels"": {{ ""fontColor"": ""#ffffff"" }}
                        }},
                        ""plugins"": {{ ""datalabels"": {{ ""display"": false }} }}
                      }}
                    }}";

        string pieUrl = await chartClient.CreateUrlAsync(new ChartRequest { Config = pieConfig, Width = 500, Height = 300 });

        embeds.Add("Detalle", new DiscordEmbedBuilder()
            .WithTitle($"Experiencia de {member.DisplayName}")
            .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
            .WithDescription($"### Total: {rank.Total.ToSpanish()} {umaPoints}\n" + string.Join("\n", detalles.OrderByDescending(x => x.Valor).Select(x => x.Detalle)))
            .WithImageUrl(pieUrl)
            .Build());
        #endregion

        #region Historial
        const int registrosMaximos = 60;
        int yearActual = DateTime.Now.Year;
        DateTime hoy = DateTime.Today;
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
        RangoEnum prevRango = DiscordHelper.GetPrevRangoByXp(minXpValue.Xp);
        long prevRangeXp = DiscordHelper.GetXpFromRango(prevRango);
        long xpSubida = rank.Total - minXpValue.Xp;
        double days = (DateTime.Today - minXpValue.Date).TotalDays;
        long maxXpChart = (long)(resultado.Max(x => x.Xp) * 1.1);

        string lineConfig = $@"{{
                      ""type"": ""line"",
                      ""data"": {{
                        ""datasets"": [
                          {{
                            ""label"": ""Experiencia"",
                            ""data"": [ {string.Join(",", resultado.Select(x => $"{x.Xp}"))} ],
                            ""fill"": true,
                            ""borderColor"": ""rgb(255, 255, 255)"",
                            ""lineTension"": 0.2,
                            ""type"": ""line"",
                            ""backgroundColor"": ""rgb(4, 172, 255)"",
                            ""borderWidth"": 3
                          }}
                        ],
                        ""labels"": [ {string.Join(",", resultado.Select(x => $"\"{x.Date:dd/MM/yyyy}\""))} ]
                      }},
                      ""options"": {{
                        ""legend"": {{ ""display"": false }},
                        ""scales"": {{
                          ""xAxes"": [ {{ ""ticks"": {{ ""fontColor"": ""#ffffff"" }}, ""gridLines"": {{ ""display"": false }} }} ],
                          ""yAxes"": [
                            {{
                              ""ticks"": {{
                                ""beginAtZero"": true,
                                ""fontColor"": ""#ffffff"",
                                ""min"": {prevRangeXp},
                                ""max"": {maxXpChart}
                              }},
                              ""gridLines"": {{ ""color"": ""rgba(255, 255, 255, 1)"", ""zeroLineColor"": ""rgba(255, 255, 255, 1)"" }}
                            }}
                          ]
                        }}
                      }}
                    }}";

        string descHistorial =
            $"### Tienes {rank.Total.ToSpanish()} {umaPoints}\n\n- Tu rango actual es {role.Mention}\n" +
            $"- Empezaste teniendo **{minXpValue.Xp.ToSpanish()} {umaPoints}** ({minXpValue.Date.Day}/{minXpValue.Date.Month}/{minXpValue.Date.Year})\n" +
            $"- En {days.ToSpanish()} dias subiste **{xpSubida.ToSpanish()} {umaPoints}**";

        if (nextRango != prevRango && nextRango != DiscordHelper.GetPrevRangoByXp(rank.Total))
            descHistorial += $"\n- El siguiente rango es **{((Enum)nextRango).GetDescription()}** llegando a **{nextRangeXp.ToSpanish()} {umaPoints}**";

        string lineUrl = await chartClient.CreateUrlAsync(new ChartRequest { Config = lineConfig, Width = 500, Height = 300 });

        embeds.Add("Historial", new DiscordEmbedBuilder()
            .WithTitle($"Experiencia de {member.DisplayName}")
            .WithDescription(descHistorial)
            .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
            .WithImageUrl(lineUrl)
            .Build());
        #endregion

        await DiscordHelper.SwitchTabsAsync(ctx, embeds);
    }

    [Command("topchart")]
    [Description("Muestra el ranking de experiencia del top en forma de chart")]
    public async Task TopChart(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        List<UserXp> serverRanking = xpState.GetGuildXp(ctx.Guild!).OrderByDescending(x => x.Total).ToList();

        List<string> colors =
        [
            "rgb(230, 124, 115)", "rgb(247, 203, 77)", "rgb(65, 179, 117)", "rgb(123, 170, 247)", "rgb(186, 103, 200)"
        ];

        int start = 0;
        UserXp? self = serverRanking.FirstOrDefault(x => x.UserId == (long)ctx.User.Id);
        if (self != null)
        {
            int indexOf = serverRanking.IndexOf(self);
            start = indexOf % 10 <= 5 ? indexOf / 10 * 10 : (indexOf / 10 + 1) * 10;
        }

        bool first = true;
        List<UserDailyXp> chartHistoryLabels = [];

        while (true)
        {
            List<UserXp> rankings = serverRanking.Skip(start).Take(5).ToList();

            List<UserDailyXp> chartTmp = [];
            foreach (UserXp rnk in rankings)
                chartTmp.AddRange(await xpState.GetUserChartHistory((ulong)rnk.UserId, true));

            if (chartTmp.Count == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No hay datos de experiencia para mostrar."));
                return;
            }

            long minXpValue = NumberHelper.ObtenerMultiploAnterior(chartTmp.Min(x => x.Xp), 1000);
            long maxXpValue = NumberHelper.ObtenerMultiploSiguiente(chartTmp.Max(x => x.Xp), 1000);

            string datasets = string.Empty;
            int it = 0;
            colors.Shuffle();
            foreach (UserXp ranking in rankings)
            {
                if (!ctx.Guild!.Members.TryGetValue((ulong)ranking.UserId, out DiscordMember? member))
                    continue;

                List<UserDailyXp> chartHistory = await xpState.GetUserChartHistory(member.Id, true);
                chartHistory.Add(new UserDailyXp { Xp = ranking.Total });

                if (first)
                {
                    chartHistoryLabels = chartHistory;
                    first = false;
                }

                datasets += $@"{{
                            ""label"": ""{member.DisplayName}"",
                            ""data"": [ {string.Join(",", chartHistory.Select(x => $"{x.Xp}"))} ],
                            ""fill"": false,
                            ""borderColor"": ""{colors[it]}"",
                            ""lineTension"": 0.2,
                            ""type"": ""line"",
                            ""backgroundColor"": ""rgb(4, 172, 255)"",
                            ""borderWidth"": 3
                            }},";
                it++;
            }

            string chartConfig = $@"{{
                      ""type"": ""line"",
                      ""data"": {{
                        ""datasets"": [ {datasets} ],
                        ""labels"": [ {string.Join(",", chartHistoryLabels.Select(x => $"\"{x.Date.Day}/{x.Date.Month}/{x.Date.Year}\""))} ]
                      }},
                      ""options"": {{
                        ""legend"": {{ ""display"": true, ""position"": ""top"", ""labels"": {{ ""fontSize"": 20, ""fontColor"": ""#ffffff"", ""fontStyle"": ""bold"" }} }},
                        ""scales"": {{
                          ""xAxes"": [ {{ ""display"": false }} ],
                          ""yAxes"": [
                            {{
                              ""ticks"": {{
                                ""beginAtZero"": true,
                                ""fontSize"": 20,
                                ""fontColor"": ""#ffffff"",
                                ""fontStyle"": ""bold"",
                                ""min"": {minXpValue},
                                ""max"": {maxXpValue}
                              }},
                              ""gridLines"": {{ ""color"": ""rgba(255, 255, 255, 1)"", ""zeroLineColor"": ""rgba(255, 255, 255, 1)"" }}
                            }}
                          ]
                        }}
                      }}
                    }}";

            byte[] image = await chartClient.RenderAsync(new ChartRequest { Config = chartConfig, Width = 1000, Height = 600 });
            string fileName = $"{StringHelper.CreateString(10)}.png";

            int end = start + 5;
            DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddActionRowComponent(
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "anterior", "Anterior"),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "siguiente", "Siguiente"))
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Top 10 de experiencia")
                    .WithDescription($"Puestos del {start} al {end}")
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

    [Command("toppaises")]
    [Description("Muestra el ranking de experiencia del servidor por pais")]
    public async Task TopPaises(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        Random rand = new();
        List<UserXp> serverRanking = xpState.GetGuildXp(ctx.Guild!);
        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);

        Dictionary<DiscordRole, long> xpPerCountry = [];
        foreach (UserXp ranking in serverRanking)
        {
            if (!ctx.Guild!.Members.TryGetValue((ulong)ranking.UserId, out DiscordMember? member)) continue;

            DiscordRole? pais = discordHelper.GetMemberPais(member);
            if (pais == null) continue;

            xpPerCountry[pais] = xpPerCountry.GetValueOrDefault(pais) + ranking.Total;
        }

        List<KeyValuePair<DiscordRole, long>> list = xpPerCountry.OrderByDescending(x => x.Value).ToList();

        KeyValuePair<DiscordRole, long> otrosItem = list.First(x => x.Key.Id == OtrosPaisRoleId);
        list.Remove(otrosItem);

        List<KeyValuePair<DiscordRole, long>> top10 = list.Take(10).ToList();
        long otrosXpCount = list.Skip(10).Sum(x => x.Value) + otrosItem.Value;

        Dictionary<DiscordRole, long> dict = top10.ToDictionary(x => x.Key, x => x.Value);
        dict[otrosItem.Key] = otrosXpCount;
        top10 = dict.OrderByDescending(x => x.Value).ToList();

        List<(string Nombre, long Valor)> res = top10
            .Select(x => (x.Key.Id == OtrosPaisRoleId ? "Otros" : x.Key.Name, x.Value))
            .ToList();
        res.RemoveAt(res.Count - 1);

        List<string> chartColors = top10.Select(_ => $"#{rand.Next(0x1000000):X6}").ToList();

        string chartConfig = $@"{{
                      ""type"": ""pie"",
                      ""data"": {{
                        ""datasets"": [
                          {{
                            ""data"": [ {string.Join(",", res.Select(x => $"{x.Valor}"))} ],
                            ""backgroundColor"": [ {string.Join(",", chartColors.Select(x => $"\"{x}\""))} ],
                            ""label"": ""Dataset 1"",
                            ""type"": ""pie"",
                            ""borderColor"": [ {string.Join(",", chartColors.Select(x => $"\"{x}\""))} ],
                            ""borderWidth"": 3
                          }}
                        ],
                        ""labels"": [ {string.Join(",", res.Select(x => $"\"{x.Nombre}\""))} ]
                      }},
                      ""options"": {{
                        ""legend"": {{ ""display"": true, ""position"": ""top"", ""labels"": {{ ""fontColor"": ""#ffffff"" }} }},
                        ""plugins"": {{ ""datalabels"": {{ ""display"": false }} }}
                      }}
                    }}";

        byte[] image = await chartClient.RenderAsync(new ChartRequest { Config = chartConfig, Width = 500, Height = 300 });

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Top de experiencia por país")
                .WithDescription(string.Join("\n", list.Select(x => $"**{x.Key.Name}**: {x.Value.ToSpanish()} {umaPoints}")))
                .WithImageUrl("attachment://xpchartcountries.png"))
            .AddFile("xpchartcountries.png", image.ToMemoryStream()));
    }

    [Command("topcotorreo")]
    [Description("Muestra el ranking de experiencia por dia")]
    public async Task TopCotorreo(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        if (DateTime.Today is { Day: 1, Month: 1 } && ctx.User.Id != config.OwnerId)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription("Este comando no está disponible el 1 de enero de cada año ya que no hay registros para hacer el top de cotorreo")
                .WithColor(DiscordColor.Red)));
            return;
        }

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);
        Dictionary<DiscordMember, double> promediosByUser = [];

        foreach (DiscordMember member in ctx.Guild!.Members.Values)
        {
            if (member.IsBot) continue;
            List<UserDailyXp> chartHistory = await xpState.GetUserChartHistory(member.Id);
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

    [Command("listaproxrango")]
    [Description("Muestra las personas que estan por subir de rango")]
    public async Task ProxPorSubirRango(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        DiscordEmoji umaPoints = await UmaPointsAsync(ctx);
        List<UserXp> xp = xpState.GetGuildXp(ctx.Guild!);
        List<(DiscordMember Member, long NextXp, RangoEnum NextRango)> ret = [];

        foreach (UserXp member in xp)
        {
            RangoEnum nextRango = DiscordHelper.GetNextRangoByXp(member.Total);
            long nextXp = DiscordHelper.GetXpFromRango(nextRango) - member.Total;

            if (nextXp <= 0 || !ctx.Guild!.Members.TryGetValue((ulong)member.UserId, out DiscordMember? user)) continue;
            if (user.Roles.Any(x => x.Id == config.Roles.Inactivo)) continue;

            bool proxSubir = nextRango switch
            {
                RangoEnum.Tama => nextXp < 500,
                RangoEnum.Casual => nextXp < 1000,
                RangoEnum.Kouhai or RangoEnum.Senpai => nextXp < 2500,
                RangoEnum.Hikikomori or RangoEnum.Sensei => nextXp < 5000,
                RangoEnum.Ousama => nextXp < 7500,
                RangoEnum.Teiou => nextXp < 10000,
                _ => false
            };

            if (proxSubir) ret.Add((user, nextXp, nextRango));
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
        await ctx.DeferResponseAsync();

        DiscordMember member = ctx.Member!;
        DiscordEmoji tenshiEmote = DiscordEmoji.FromGuildEmote(ctx.Client, TenshiEmoteId);
        string desc = string.Empty;

        if (discordHelper.RangoAPartirDe(ctx.Guild!, member, RangoEnum.Tama, false))
            desc += "### 🥚 Tama:\n- Participar en los intercambios\n- Adjuntar archivos\n\n";

        if (discordHelper.RangoAPartirDe(ctx.Guild!, member, RangoEnum.Kouhai, false))
            desc += "### 🍙 Kouhai:\n- Entrada garantizada a eventos del servidor\n\n";

        if (discordHelper.RangoAPartirDe(ctx.Guild!, member, RangoEnum.Sensei, false))
            desc += "### 🍜 Senpai:\n- Elegir entre 45 colores para tu usuario\n\n";

        if (discordHelper.RangoAPartirDe(ctx.Guild!, member, RangoEnum.Ousama, false))
            desc += "### 👑 Ousama:\n- Canal de voz propio\n\n";

        if (discordHelper.RangoAPartirDe(ctx.Guild!, member, RangoEnum.Teiou, false))
            desc += $"### 🥕 Teiou:\n- Escribir en {ctx.Guild!.Channels[TeiouChannelId].Mention}\n- Comando {Formatter.InlineCode("/teiou nickname")}\n\n";

        if (member.PremiumSince != null)
            desc += $"### 😇 Tenshi:\n- Emotes exclusivos `{tenshiEmote}`\n- 1 a 3 de XP extra por mensaje\n\n";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithTitle("Beneficios desbloqueados")
            .WithDescription(string.IsNullOrEmpty(desc) ? "Sin beneficios desbloqueados" : desc)
            .WithAuthor(member.DisplayName, iconUrl: member.AvatarUrl)
            .WithColor(DiscordColor.Blurple)));
    }

    private readonly record struct RankEntry(int Rank, long Score, ulong UserId);
}
