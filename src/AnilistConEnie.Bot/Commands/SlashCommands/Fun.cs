using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

//[TestCommand]
public class Fun(
    BotConfiguration config,
    DiscordHelper discordHelper,
    FunHelper funHelper,
    BoluditosState boluditosState,
    ConfessionsState confessionsState,
    IImagenStorageRepository imagenStorageRepository,
    IUsuariosDiscordRepository usuariosDiscordRepository,
    IChartClient chartClient,
    IHttpClientFactory httpClientFactory,
    ILogger<Fun> logger)
{
    private const string FrameLove = "frame-love.png";
    private const string FuckMarryKillTemplate = "fuckmarrykill.png";
    private const string BoluditoImage = "https://media.discordapp.net/attachments/1106702589359292416/1293580545296568341/1258107870340448369.png";

    private static string ImagePath(string fileName) => Path.Join(AppContext.BaseDirectory, "Images", fileName);

    [Command("fakesay")]
    [Description("Usurpa la identidad de un usuario y di algo en su nombre")]
    public async Task FakeSay(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario del que quieres usurpar su identidad")] DiscordUser usuario,
        [Parameter("Mensaje")] [Description("El mensaje a replicar")] string mensaje)
    {
        await ctx.DeferResponseAsync(true);

        if (ctx.Member is null || !ctx.Member.Permissions.HasPermission(DiscordPermission.ManageGuild))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Sin permiso",
                Description = "Necesitas el permiso de `Gestionar servidor` para usar este comando.",
                Color = DiscordColor.Red
            }));
            return;
        }

        DiscordMember member = await ctx.Guild!.GetMemberAsync(usuario.Id);

        // Solo se puede ejecutar un webhook si tenemos su token (los creados por otras apps no lo exponen).
        DiscordWebhook webhook = (await ctx.Channel.GetWebhooksAsync()).FirstOrDefault(wbhk => wbhk.Name == "AnilistConEnie" && !string.IsNullOrEmpty(wbhk.Token))
                                 ?? await ctx.Channel.CreateWebhookAsync("AnilistConEnie");

        DiscordWebhookBuilder wBuilder = new DiscordWebhookBuilder()
            .WithContent(mensaje)
            .WithAvatarUrl(member.GuildAvatarUrl ?? member.AvatarUrl)
            .WithUsername(member.DisplayName)
            .AddMention(new UserMention());

        try
        {
            await webhook.ExecuteAsync(wBuilder);
        }
        catch (NotFoundException)
        {
            // El webhook cacheado ya no existe en Discord: recreamos y reintentamos.
            webhook = await ctx.Channel.CreateWebhookAsync("AnilistConEnie");
            await webhook.ExecuteAsync(wBuilder);
        }

        await ctx.Interaction.DeleteOriginalResponseAsync();
    }

    [Command("ship")]
    [Description("Elegir la ship de un usuario")]
    public async Task Ship(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario del que quieres ver su ship")] DiscordUser? usuario = null)
    {
        await ctx.DeferResponseAsync();

        usuario ??= ctx.User;
        DiscordMember elegido = ElegirMiembroAleatorio(ctx, usuario.Id);
        DiscordMember ctxMiembro = await ctx.Guild!.GetMemberAsync(usuario.Id);

        byte[] imagen = await GenerarImagenShipAsync(usuario.GetAvatarUrl(MediaFormat.Png, 512), elegido.GetAvatarUrl(MediaFormat.Png, 512));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Shippeo",
            Description = $"Shippeo a {ctxMiembro.Mention} con **{elegido.Mention}** 💘",
            ImageUrl = "attachment://imagen.png"
        }).AddFile("imagen.png", imagen.ToMemoryStream()));
    }

    [Command("shiprandom")]
    [Description("Elijo una ship del servidor")]
    public async Task ShipRandom(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        DiscordMember elegido1 = ElegirMiembroAleatorio(ctx, ctx.User.Id);
        DiscordMember elegido2;
        do
        {
            elegido2 = ElegirMiembroAleatorio(ctx, ctx.User.Id);
        } while (elegido1.Id == elegido2.Id);

        byte[] imagen = await GenerarImagenShipAsync(elegido1.GetAvatarUrl(MediaFormat.Png, 512), elegido2.GetAvatarUrl(MediaFormat.Png, 512));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Shippeo Random",
            Description = $"Shippeo a {elegido1.Mention} con **{elegido2.Mention}** 💘",
            ImageUrl = "attachment://imagen.png"
        }).AddFile("imagen.png", imagen.ToMemoryStream()));
    }

    [Command("truelove")]
    [Description("Elige el amor verdadero de un usuario")]
    public async Task TrueLove(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario del que quieres ver su ship")] DiscordUser usuario)
    {
        await ctx.DeferResponseAsync();

        int maxPorcentaje = 0;
        DiscordMember match = ctx.Member!;
        List<(DiscordMember Miembro, int Porcentaje)> amorios = [];

        foreach (KeyValuePair<ulong, DiscordMember> member in ctx.Guild!.Members)
        {
            if (member.Value.IsBot || member.Value.Id == usuario.Id || !discordHelper.RangoAPartirDe(ctx.Guild, member.Value, RangoEnum.Tama, true))
                continue;

            Random rnd = new((int)(usuario.Id + member.Key));
            int porcentajeAmor = rnd.Next(0, 101);
            amorios.Add((member.Value, porcentajeAmor));

            if (porcentajeAmor > maxPorcentaje)
            {
                maxPorcentaje = porcentajeAmor;
                match = member.Value;
            }
        }

        List<(DiscordMember Miembro, int Porcentaje)> amores = amorios.OrderByDescending(x => x.Porcentaje).Take(5).ToList();
        string amoriosStr = $"**Top 5 pretendientes:**\n{string.Join("\n", amores.Select(x => $"- **{x.Miembro.DisplayName}** con un **{x.Porcentaje}%**"))}";

        List<(DiscordMember Miembro, int Porcentaje)> odiados = amorios.OrderBy(x => x.Porcentaje).Take(5).ToList();
        string odiadosStr = $"**Top 5 odiados:**\n{string.Join("\n", odiados.Select(x => $"- **{x.Miembro.DisplayName}** con un **{x.Porcentaje}%**"))}";

        byte[] imagen = await GenerarImagenShipAsync(usuario.GetAvatarUrl(MediaFormat.Png, 512), match.GetAvatarUrl(MediaFormat.Png, 512));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "True love",
            Description = $"El amor verdadero de {ctx.Guild.Members[usuario.Id].DisplayName} es **{match.DisplayName}** con un **{maxPorcentaje}%** 💘\n\n{amoriosStr}\n\n{odiadosStr}",
            ImageUrl = "attachment://imagen.png",
            Color = DiscordColor.HotPink
        }).AddFile("imagen.png", imagen.ToMemoryStream()));
    }

    [Command("subirimagen")]
    [Description("Sube una imagen")]
    public async Task SubirImagen(
        SlashCommandContext ctx,
        [Parameter("Imagen")] [Description("Imagen a subir")] DiscordAttachment imagen)
    {
        await ctx.DeferResponseAsync(true);

        if (imagen.MediaType is null || !imagen.MediaType.StartsWith("image"))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription("El archivo debe ser una imagen")
                .WithColor(DiscordColor.Red)));
            return;
        }

        HttpClient client = httpClientFactory.CreateClient();
        byte[] bytes = await client.GetByteArrayAsync(imagen.Url);
        using MemoryStream stream = new(bytes);
        string fileName = StringHelper.CreateString(10);
        string newUrl = await imagenStorageRepository.UploadImageAsync(stream, fileName, ctx.User.Id);

        if (!string.IsNullOrEmpty(newUrl))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Imagen subida con exito")
                .WithDescription($"## Url:\n{Formatter.BlockCode(newUrl)}")
                .WithImageUrl(imagen.Url)
                .WithColor(DiscordColor.Green)));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription("No se pudo subir la imagen")
                .WithColor(DiscordColor.Red)));
        }
    }

    [Command("fuckmarrykill")]
    [Description("Juego de cojer casarse o matar")]
    public async Task FuckMarryKill(
        SlashCommandContext ctx,
        [Parameter("SoloMiembrosActivos")] [Description("No saldran usuarios con el rol Inactivo")] bool activo = true)
    {
        await ctx.DeferResponseAsync();

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        Random rnd = new();

        List<DiscordMember> miembros = ctx.Guild!.Members.Values
            .Where(x => !x.IsBot && x.Id != ctx.User.Id && discordHelper.RangoAPartirDe(ctx.Guild, x, RangoEnum.Tama, activo))
            .ToList();

        if (miembros.Count < 3)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No hay suficientes miembros para jugar."));
            return;
        }

        DiscordMember elegido1 = miembros[rnd.Next(miembros.Count)];
        DiscordMember elegido2;
        do { elegido2 = miembros[rnd.Next(miembros.Count)]; } while (elegido2.Id == elegido1.Id);
        DiscordMember elegido3;
        do { elegido3 = miembros[rnd.Next(miembros.Count)]; } while (elegido3.Id == elegido1.Id || elegido3.Id == elegido2.Id);

        List<DiscordMember> elegidos = [elegido1, elegido2, elegido3];

        DiscordMessage elegirMsg = await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Cojer, casarse o matar")
                .WithDescription($"{ctx.User.Mention}, elige a quien te quieres cojer, casarte o matar:\n\n" +
                                 $"- {elegido1.DisplayName}\n- {elegido2.DisplayName}\n- {elegido3.DisplayName}"))
            .AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Primary, "btnElegirFMK", "Elegir")));

        InteractivityResult<ComponentInteractionCreatedEventArgs> btnInteraction =
            await interactivity.WaitForButtonAsync(elegirMsg, ctx.User, TimeSpan.FromMinutes(5));

        if (btnInteraction.TimedOut)
        {
            await ctx.Interaction.DeleteOriginalResponseAsync();
            return;
        }

        const string placeholder = "Escribe 1 para cojer/follar, 2 para casarse o 3 para matar";
        string modalId = $"modal-{btnInteraction.Result.Interaction.Id}";

        DiscordModalBuilder modal = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Cojer, casarse o matar");

        foreach (DiscordMember usr in elegidos)
        {
            modal.AddTextInput(
                new DiscordTextInputComponent($"{usr.Id}", placeholder, null, true, DiscordTextInputStyle.Short, 1, 1),
                usr.DisplayName);
        }

        await btnInteraction.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        InteractivityResult<ModalSubmittedEventArgs> modalInteraction =
            await interactivity.WaitForModalAsync(modalId, ctx.User, TimeSpan.FromMinutes(5));

        if (modalInteraction.TimedOut)
        {
            await ctx.Interaction.DeleteOriginalResponseAsync();
            return;
        }

        await modalInteraction.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        ulong cojer = 0, casarse = 0, matar = 0;
        foreach (KeyValuePair<string, IModalSubmission> valor in modalInteraction.Result.Values)
        {
            string respuesta = valor.Value is TextInputModalSubmission texto ? texto.Value : string.Empty;
            switch (respuesta)
            {
                case "1":
                    if (cojer != 0) { await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Solo te podes cojer a uno")); return; }
                    cojer = ulong.Parse(valor.Key);
                    break;
                case "2":
                    if (casarse != 0) { await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Solo podes casarte con uno")); return; }
                    casarse = ulong.Parse(valor.Key);
                    break;
                case "3":
                    if (matar != 0) { await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Solo podes matar a uno")); return; }
                    matar = ulong.Parse(valor.Key);
                    break;
                default:
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Los valores deben ser 1, 2 o 3"));
                    return;
            }
        }

        DiscordMember usrCasarte = elegidos.First(x => x.Id == casarse);
        DiscordMember usrCojer = elegidos.First(x => x.Id == cojer);
        DiscordMember usrMatar = elegidos.First(x => x.Id == matar);

        HttpClient client = httpClientFactory.CreateClient();
        byte[] imagen = File.ReadAllBytes(ImagePath(FuckMarryKillTemplate));
        imagen = ImageHelper.DrawIntoImage(imagen, await client.GetByteArrayAsync(usrCasarte.GuildAvatarUrl ?? usrCasarte.AvatarUrl), 26, 26);
        imagen = ImageHelper.DrawIntoImage(imagen, await client.GetByteArrayAsync(usrCojer.GuildAvatarUrl ?? usrCojer.AvatarUrl), 643, 26);
        imagen = ImageHelper.DrawIntoImage(imagen, await client.GetByteArrayAsync(usrMatar.GuildAvatarUrl ?? usrMatar.AvatarUrl), 1260, 26);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"# Cojer, casarse o matar\n\n- Se casó con {usrCasarte.DisplayName}\n- Se cojió/folló a {usrCojer.DisplayName}\n- Mató a {usrMatar.DisplayName}\n")
            .AddFile("imagen.png", imagen.ToMemoryStream()));
    }

    [Command("boludometro")]
    [Description("Descubre que tan boludito estas hoy")]
    public async Task Boludometro(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        DiscordMember member = ctx.Member!;

        int seed = ctx.User.Id.GetHashCode() ^ (DateTime.Now.Year * 100 + DateTime.Now.Month);
        Random rnd = new(seed);

        int diaHoy = DateTime.Now.Day;
        int totalDiasDelMes = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

        int cambiosTendencia = rnd.Next(2, 6);
        Dictionary<int, int> puntosPorDia = new();

        List<int> segmentos = [];
        int puntosRestantes = totalDiasDelMes - 1;
        for (int i = 0; i < cambiosTendencia && puntosRestantes > 0; i++)
        {
            int maxPermitido = puntosRestantes - (cambiosTendencia - i - 1) * 3;
            if (maxPermitido < 3) break;
            int seg = rnd.Next(3, maxPermitido + 1);
            segmentos.Add(seg);
            puntosRestantes -= seg;
        }
        if (puntosRestantes > 0) segmentos.Add(puntosRestantes);

        int actual = rnd.Next(0, 101);
        int dia = 1;
        puntosPorDia[dia++] = actual;
        bool subiendo = rnd.Next(2) == 0;

        foreach (int seg in segmentos)
        {
            int objetivo = subiendo ? rnd.Next(actual + 1, 101) : rnd.Next(0, actual);
            for (int i = 1; i <= seg; i++)
            {
                if (dia > diaHoy) break;
                float t = (float)i / seg;
                int valor = (int)(actual + (objetivo - actual) * t + rnd.Next(-3, 4));
                puntosPorDia[dia++] = Math.Clamp(valor, 0, 100);
            }
            if (dia > diaHoy) break;
            actual = puntosPorDia[dia - 1];
            subiendo = !subiendo;
        }

        int value = puntosPorDia.Last().Value;

        if (value == 100 && !boluditosState.IsBoludito(ctx.User.Id))
        {
            boluditosState.AddBoludito(ctx.User.Id);
            await ctx.Guild!.Channels[config.Channels.General].SendMessageAsync(new DiscordEmbedBuilder()
                .WithTitle($"{member.DisplayName} ES UN BOLUDITO")
                .WithImageUrl(BoluditoImage)
                .WithColor(DiscordColor.Gold));
        }

        string gaugeConfig = $$"""
        {
            type: 'gauge',
            data: {
                labels: ['Normal', 'Boludo', 'Boludito'],
                datasets: [{ data: [50, 100], value: {{value}}, minValue: 0, backgroundColor: ['green', 'red'], borderWidth: 4 }]
            },
            options: {
                legend: { display: false },
                title: { display: false, text: 'Boludometro' },
                needle: { radiusPercentage: 1, widthPercentage: 1, lengthPercentage: 60, color: '#fff' },
                valueLabel: { fontSize: 35, backgroundColor: 'transparent', color: '#00F0FF', formatter: function (value, context) { return value + '%'; }, bottomMarginPercentage: 10 },
                plugins: { datalabels: { display: 'auto', formatter: function (value, context) { return context.chart.data.labels[context.dataIndex]; }, color: '#fff' } }
            }
        }
        """;

        double promedio = puntosPorDia.Values.Average();
        KeyValuePair<int, int> max = puntosPorDia.MaxBy(x => x.Value);
        KeyValuePair<int, int> min = puntosPorDia.MinBy(x => x.Value);

        string lineConfig = $$"""
        {
            type: 'line',
            data: {
                labels: [ {{string.Join(",", puntosPorDia.Keys)}} ],
                datasets: [{ label: 'Boludómetro de {{member.DisplayName}}', backgroundColor: 'rgb(255, 99, 132)', borderColor: 'rgb(255, 99, 132)', data: [ {{string.Join(",", puntosPorDia.Values)}} ], fill: false }]
            },
            "options": { "scales": { "yAxes": [ { "ticks": { "min": 0, "max": 100 } } ] } }
        }
        """;

        string gaugeUrl = await chartClient.CreateUrlAsync(new ChartRequest { Config = gaugeConfig, Width = 500, Height = 300, BackgroundColor = "transparent" });
        string lineUrl = await chartClient.CreateUrlAsync(new ChartRequest { Config = lineConfig, Width = 500, Height = 300, BackgroundColor = "transparent" });

        char genero = funHelper.GetGenero(member);

        DiscordEmbed embedDiario = new DiscordEmbedBuilder()
            .WithTitle("Boludómetro")
            .WithDescription(funHelper.BoluditoLevel(ctx.Client, member, value))
            .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
            .WithImageUrl(gaugeUrl)
            .Build();

        DiscordEmbed embedHistorial = new DiscordEmbedBuilder()
            .WithTitle("Boludómetro")
            .WithDescription(
                $"**{member.DisplayName}** este mes fue un {promedio:0}% bolud{genero}\n\n" +
                $"- Su día de menor boludez fue el **{min.Key}/{DateTime.Now.Month}** siendo un **{min.Value:0}% bolud{genero}**\n" +
                $"- Su día de mayor boludez fue el **{max.Key}/{DateTime.Now.Month}** siendo un **{max.Value:0}% bolud{genero}**")
            .WithThumbnail(member.GuildAvatarUrl ?? member.AvatarUrl)
            .WithFooter("La gráfica se resetea mensualmente.")
            .WithImageUrl(lineUrl)
            .Build();

        Dictionary<string, DiscordEmbed> tabs = new()
        {
            { "Diario", embedDiario },
            { "Historial", embedHistorial }
        };

        await DiscordHelper.SwitchTabsAsync(ctx, tabs);
    }

    [Command("horoscopo")]
    [Description("Tu horoscopo diario")]
    public async Task Horoscopo(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        List<UserCumple> birthdays = await usuariosDiscordRepository.GetBirthdays(false);
        UserCumple? birthday = birthdays.Find(x => x.Id == (long)ctx.Member!.Id);

        if (birthday is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle("Error")
                .WithDescription("No tienes registrado tu cumpleaños en el servidor.")));
            return;
        }

        SignoZodiacal signo = FunHelper.GetSignoByBirthday(birthday.Birthday.Day, birthday.Birthday.Month);
        string signoStrEnglish = signo.ToString().ToLowerInvariant();
        string signoStr = ((Enum)signo).GetDescription();
        DiscordEmoji emote = FunHelper.EmoteOfSignoZodiacal(signo);

        string diaString = DateTime.Today.ToString("yyyy-MM-dd");
        HttpClient client = httpClientFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"https://horoscope-app-api.vercel.app/api/v1/get-horoscope/daily?sign={signoStrEnglish}&day={diaString}");

        if (!response.IsSuccessStatusCode)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle("Error")
                .WithDescription("Hubo un error tratando de obtener el horoscopo.")));
            return;
        }

        string content = await response.Content.ReadAsStringAsync();
        HoroscopoResponse? horoscopeData = JsonSerializer.Deserialize<HoroscopoResponse>(content, JsonOptions);
        if (horoscopeData?.Data is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle("Error")
                .WithDescription("Hubo un error tratando de obtener el horoscopo.")));
            return;
        }

        string horoscopo = horoscopeData.Data.HoroscopeData;
        DateTime date = DateTime.Parse(horoscopeData.Data.Date, CultureInfo.InvariantCulture);

        Random rndSignoDay = new((int)signo + date.DayOfYear + date.Year);
        Random rndMember = new((int)ctx.Member!.Id);
        Random endMemberSignoDay = new((int)signo + date.DayOfYear + date.Year + (int)ctx.Member.Id);

        double rndAmor = Math.Round(0.7 * rndSignoDay.Next(0, 101) + 0.3 * rndMember.Next(0, 101), 0);
        double rndSalud = Math.Round(0.7 * rndSignoDay.Next(0, 101) + 0.3 * rndMember.Next(0, 101), 0);
        double rndDinero = Math.Round(0.7 * rndSignoDay.Next(0, 101) + 0.3 * rndMember.Next(0, 101), 0);

        try
        {
            horoscopo = await TranslationHelper.TranslateAsync(client, horoscopo, "en", "es");
        }
        catch (Exception ex)
        {
            // Si el traductor no está disponible mostramos el texto original en inglés de la API.
            logger.LogWarning(ex, "No se pudo traducir el horoscopo, se muestra el texto original en inglés");
        }

        (string Texto, DiscordEmoji Emote) amor = FunHelper.GetHoroscopoCategoria(CategoriaHoroscopo.Amor, rndAmor, endMemberSignoDay);
        (string Texto, DiscordEmoji Emote) dinero = FunHelper.GetHoroscopoCategoria(CategoriaHoroscopo.Dinero, rndDinero, endMemberSignoDay);
        (string Texto, DiscordEmoji Emote) bienestar = FunHelper.GetHoroscopoCategoria(CategoriaHoroscopo.Bienestar, rndSalud, endMemberSignoDay);

        CultureInfo es = new("es-ES");
        string dateSpanish = date.ToString("dddd dd", es) + " de " + date.ToString("MMMM", es) + $" del {date.Year}";

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle(dateSpanish)
            .WithDescription($"# Horóscopo diario\n\n{horoscopo}\n\n" +
                $"### Amor:\n{amor.Emote} {amor.Texto}\n\n" +
                $"### Dinero:\n{dinero.Emote} {dinero.Texto}\n\n" +
                $"### Bienestar:\n{bienestar.Emote} {bienestar.Texto}\n\n" +
                $"### Envía una sugerencia {Formatter.MaskedUrl("aquí", new Uri("https://forms.gle/JMd42wysMQrQaekA6"), "Formulario de Google para enviar sugerencia de respuesta en apartados amor/dinero/bienestar.")}")
            .WithAuthor($"{signoStr} {emote}")
            .WithFooter("Este horóscopo se basa mayor parte en su signo zodiacal y en menor parte en su persona.")
            .WithThumbnail(ctx.Member.GuildAvatarUrl ?? ctx.Member.AvatarUrl);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("confesion")]
    [Description("Confiesa algo")]
    public async Task Confesion(
        SlashCommandContext ctx,
        [Parameter("Confesion")] [Description("Lo que quieres confesar")] string confesion)
    {
        await ctx.DeferResponseAsync(true);

        if (confessionsState.UserConfessed(ctx.User.Id))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle("Error")
                .WithDescription("Ya confesaste hoy!")));
            return;
        }

        List<string> imagenesUrl =
        [
            "https://media.discordapp.net/attachments/1106702589359292416/1339760645939269663/kirito-peeking-and-closing-eye-4bpv0ex5slkbib9y.webp",
            "https://media.discordapp.net/attachments/1106702589359292416/1339760646455296122/9961412_orig.gif",
            "https://media.discordapp.net/attachments/1106702589359292416/1339760647071600691/afad7e52e83cb89c4f38e942a9ccc133.gif"
        ];

        DiscordChannel channel = ctx.Guild!.Channels[config.Channels.General];
        DiscordMessage message = await channel.SendMessageAsync(new DiscordEmbedBuilder()
            .WithTitle("Nueva Confesion")
            .WithDescription(confesion)
            .WithFooter("Reacciona con una ratita si quieres saber quien lo confesó")
            .WithColor(DiscordColor.Blue)
            .WithImageUrl(imagenesUrl[NumberHelper.GetNumeroRandom(0, imagenesUrl.Count - 1)]));

        confessionsState.AddDailyConfessionUser(ctx.User.Id, message.Id);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("Confesión enviada")
            .WithDescription("Tu confesión fue publicada de forma anónima.")));
    }

    private DiscordMember ElegirMiembroAleatorio(SlashCommandContext ctx, ulong excluirId)
    {
        List<DiscordMember> miembros = ctx.Guild!.Members.Values
            .Where(x => !x.IsBot && x.Id != excluirId
                        && (ctx.Guild.Id != config.GuildId || discordHelper.RangoAPartirDe(ctx.Guild, x, RangoEnum.Tama, true)))
            .ToList();

        return miembros[Random.Shared.Next(miembros.Count)];
    }

    private async Task<byte[]> GenerarImagenShipAsync(string avatar1, string avatar2)
    {
        HttpClient client = httpClientFactory.CreateClient();
        byte[] bytes1 = await client.GetByteArrayAsync(avatar1);
        byte[] bytes2 = await client.GetByteArrayAsync(avatar2);

        byte[] merged = ImageHelper.MergeImage(bytes1, bytes2, 1024, 512);
        return ImageHelper.OverlapImage(merged, File.ReadAllBytes(ImagePath(FrameLove)), 1024, 512);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class HoroscopoResponse
    {
        [JsonPropertyName("data")] public HoroscopoData? Data { get; set; }
    }

    private sealed class HoroscopoData
    {
        [JsonPropertyName("date")] public string Date { get; set; } = string.Empty;
        [JsonPropertyName("horoscope")] public string HoroscopeData { get; set; } = string.Empty;
    }
}
