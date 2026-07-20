using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnilistConEnie.Application.Charts;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Fun;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Application.Membership;
using AnilistConEnie.Bot.Commands.Framework.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
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
using AnilistConEnie.Bot.Extensions;

namespace AnilistConEnie.Bot.Commands.Slash;

//[TestCommand]
[Command("fun")]
public class Fun(
    BotConfiguration config,
    DiscordBotService discordBotService,
    RangoRoles rangoRoles,
    FunService funService,
    BoluditosState boluditosState,
    ConfessionsState confessionsState,
    SubirImagenState subirImagenState,
    SubirImagenSettings subirImagenSettings,
    IFirebaseRepository firebaseRepository,
    IUsuariosRepository usuariosRepository,
    IChartRenderer chartRenderer,
    IHttpClientFactory httpClientFactory,
    ILogger<Fun> logger,
    InteractivityExtension interactivity)
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
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync(true);

        if (ctx.Member is null || !ctx.Member.Permissions.HasPermission(DiscordPermission.ManageGuild))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.SinPermiso()));
            return;
        }

        DiscordMember member = await ctx.Guild!.GetMemberAsync(usuario.Id);

        // Solo se puede ejecutar un webhook si tenemos su token (los creados por otras apps no lo exponen).
        DiscordWebhook webhook = (await ctx.Channel.GetWebhooksAsync()).FirstOrDefault(wbhk => wbhk.Name == "AnilistConEnie" && !string.IsNullOrEmpty(wbhk.Token))
                                 ?? await ctx.Channel.CreateWebhookAsync("AnilistConEnie");

        DiscordWebhookBuilder wBuilder = new DiscordWebhookBuilder()
            .WithContent(mensaje)
            .WithAvatarUrl(member.AvatarUrlPreferido())
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
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

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
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

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
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<DiscordMember> candidatos = ctx.Guild!.Members.Values
            .Where(m => !m.IsBot && m.Id != usuario.Id && rangoRoles.RangoAPartirDe(ctx.Guild, m, RangoEnum.Tama, true))
            .ToList();

        TrueLoveResult resultado = TrueLoveCalculator.Calcular(usuario.Id, candidatos.Select(m => m.Id));

        Dictionary<ulong, DiscordMember> porId = candidatos.ToDictionary(m => m.Id);
        string NombreDe(ulong id) => porId.TryGetValue(id, out DiscordMember? m) ? m.DisplayName : id.ToString();

        DiscordMember match = resultado.MatchId is { } matchId && porId.TryGetValue(matchId, out DiscordMember? matchMember)
            ? matchMember
            : ctx.Member!;
        int maxPorcentaje = resultado.MaxPorcentaje;

        string amoriosStr = $"**Top 5 pretendientes:**\n{string.Join("\n", resultado.Pretendientes.Select(x => $"- **{NombreDe(x.Id)}** con un **{x.Porcentaje}%**"))}";
        string odiadosStr = $"**Top 5 odiados:**\n{string.Join("\n", resultado.Odiados.Select(x => $"- **{NombreDe(x.Id)}** con un **{x.Porcentaje}%**"))}";

        byte[] imagen = await GenerarImagenShipAsync(usuario.GetAvatarUrl(MediaFormat.Png, 512), match.GetAvatarUrl(MediaFormat.Png, 512));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "True love",
            Description = $"El amor verdadero de {(ctx.Guild.Members.TryGetValue(usuario.Id, out DiscordMember? enamorado) ? enamorado.DisplayName : usuario.Username)} es **{match.DisplayName}** con un **{maxPorcentaje}%** 💘\n\n{amoriosStr}\n\n{odiadosStr}",
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
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync(true);

        if (subirImagenState.EnCooldown(ctx.User.Id, DateTime.UtcNow))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"Debes esperar {subirImagenSettings.CooldownMinutos} minutos entre subidas de imágenes")));
            return;
        }

        if (imagen.MediaType is null || !imagen.MediaType.StartsWith("image") || imagen.FileSize > subirImagenSettings.MaxTamanoBytes)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"El archivo debe ser una imagen de hasta {subirImagenSettings.MaxTamanoBytes / (1024 * 1024)} MB")));
            return;
        }

        HttpClient client = httpClientFactory.CreateClient();
        client.MaxResponseContentBufferSize = subirImagenSettings.MaxTamanoBytes;
        byte[] bytes = await client.GetByteArrayAsync(imagen.Url);

        if (!ImageHelper.EsImagenValida(bytes))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("El archivo no es una imagen válida")));
            return;
        }

        subirImagenState.RegistrarSubida(ctx.User.Id, DateTime.UtcNow.AddMinutes(subirImagenSettings.CooldownMinutos));
        using MemoryStream stream = new(bytes);
        string fileName = StringHelper.CreateString(10);
        string newUrl = await firebaseRepository.UploadImageAsync(stream, fileName, ctx.User.Id);

        if (!string.IsNullOrEmpty(newUrl))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Imagen subida con exito")
                .WithDescription($"## Url:\n{Formatter.BlockCode(newUrl)}")
                .WithImageUrl(imagen.Url!)
                .WithColor(DiscordColor.Green)));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("No se pudo subir la imagen")));
        }
    }

    [Command("fuckmarrykill")]
    [Description("Juego de cojer casarse o matar")]
    public async Task FuckMarryKill(
        SlashCommandContext ctx,
        [Parameter("SoloMiembrosActivos")] [Description("No saldran usuarios con el rol Inactivo")] bool activo = true)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        Random rnd = Random.Shared;

        List<DiscordMember> miembros = ctx.Guild!.Members.Values
            .Where(x => !x.IsBot && x.Id != ctx.User.Id && rangoRoles.RangoAPartirDe(ctx.Guild, x, RangoEnum.Tama, activo))
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
        imagen = ImageHelper.DrawIntoImage(imagen, await client.GetByteArrayAsync(usrCasarte.AvatarUrlPreferido()), 26, 26);
        imagen = ImageHelper.DrawIntoImage(imagen, await client.GetByteArrayAsync(usrCojer.AvatarUrlPreferido()), 643, 26);
        imagen = ImageHelper.DrawIntoImage(imagen, await client.GetByteArrayAsync(usrMatar.AvatarUrlPreferido()), 1260, 26);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"# Cojer, casarse o matar\n\n- Se casó con {usrCasarte.DisplayName}\n- Se cojió/folló a {usrCojer.DisplayName}\n- Mató a {usrMatar.DisplayName}\n")
            .AddFile("imagen.png", imagen.ToMemoryStream()));
    }

    [Command("boludometro")]
    [Description("Descubre que tan boludito estas hoy")]
    public async Task Boludometro(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        DiscordMember member = ctx.Member!;

        DateTime hoy = RelojServidor.Hoy;
        Random rnd = new(BoludometroCalculator.Seed(ctx.User.Id, hoy.Year, hoy.Month));
        Dictionary<int, int> puntosPorDia = BoludometroCalculator.GenerarHistorial(
            rnd, hoy.Day, DateTime.DaysInMonth(hoy.Year, hoy.Month));

        int value = puntosPorDia.Last().Value;

        if (value == 100 && !boluditosState.IsBoludito(ctx.User.Id))
        {
            boluditosState.AddBoludito(ctx.User.Id);
            await ctx.Guild!.Channels[config.Channels.General].SendMessageAsync(new DiscordEmbedBuilder()
                .WithTitle($"{member.DisplayName} ES UN BOLUDITO")
                .WithImageUrl(BoluditoImage)
                .WithColor(DiscordColor.Gold));
        }

        double promedio = puntosPorDia.Values.Average();
        KeyValuePair<int, int> max = puntosPorDia.MaxBy(x => x.Value);
        KeyValuePair<int, int> min = puntosPorDia.MinBy(x => x.Value);

        byte[] gaugeImage = await chartRenderer.RenderAsync(FunCharts.BoludoGauge(value));
        byte[] lineImage = await chartRenderer.RenderAsync(FunCharts.BoludoLine(member.DisplayName, puntosPorDia.Keys, puntosPorDia.Values));
        const string gaugeFile = "boludometrogauge.png";
        const string lineFile = "boludometrohistorial.png";

        char genero = funService.GetGenero(member);
        DiscordEmoji loreaEste = await DiscordEmojiHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.Bot.LoreaEste);

        DiscordEmbed embedDiario = new DiscordEmbedBuilder()
            .WithTitle("Boludómetro")
            .WithDescription(funService.BoluditoLevel(loreaEste, member, value))
            .WithThumbnail(member.AvatarUrlPreferido())
            .WithImageUrl($"attachment://{gaugeFile}")
            .Build();

        DiscordEmbed embedHistorial = new DiscordEmbedBuilder()
            .WithTitle("Boludómetro")
            .WithDescription(
                $"**{member.DisplayName}** este mes fue un {promedio:0}% bolud{genero}\n\n" +
                $"- Su día de menor boludez fue el **{min.Key}/{hoy.Month}** siendo un **{min.Value:0}% bolud{genero}**\n" +
                $"- Su día de mayor boludez fue el **{max.Key}/{hoy.Month}** siendo un **{max.Value:0}% bolud{genero}**")
            .WithThumbnail(member.AvatarUrlPreferido())
            .WithFooter("La gráfica se resetea mensualmente.")
            .WithImageUrl($"attachment://{lineFile}")
            .Build();

        Dictionary<string, DiscordInteractivity.TabContent> tabs = new()
        {
            { "Diario", new DiscordInteractivity.TabContent(embedDiario, gaugeImage, gaugeFile) },
            { "Historial", new DiscordInteractivity.TabContent(embedHistorial, lineImage, lineFile) }
        };

        await DiscordInteractivity.SwitchTabsAsync(ctx, tabs);
    }

    [Command("horoscopo")]
    [Description("Tu horoscopo diario")]
    public async Task Horoscopo(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<Usuario> cumples = await usuariosRepository.GetCumples();
        List<UserCumple> birthdays = CumpleCalculator.Proximos(cumples, RelojServidor.Ahora, false);
        UserCumple? birthday = birthdays.Find(x => x.Id == (long)ctx.Member!.Id);

        if (birthday is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("No tienes registrado tu cumpleaños en el servidor.")));
            return;
        }

        SignoZodiacal signo = FunService.GetSignoByBirthday(birthday.Birthday.Day, birthday.Birthday.Month);
        string signoStrEnglish = signo.ToString().ToLowerInvariant();
        string signoStr = ((Enum)signo).GetDescription();
        DiscordEmoji emote = FunService.EmoteOfSignoZodiacal(signo);

        string diaString = RelojServidor.Hoy.ToString("yyyy-MM-dd");
        HttpClient client = httpClientFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"https://horoscope-app-api.vercel.app/api/v1/get-horoscope/daily?sign={signoStrEnglish}&day={diaString}");

        if (!response.IsSuccessStatusCode)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("Hubo un error tratando de obtener el horoscopo.")));
            return;
        }

        string content = await response.Content.ReadAsStringAsync();
        HoroscopoResponse? horoscopeData = JsonSerializer.Deserialize<HoroscopoResponse>(content, JsonOptions);
        if (horoscopeData?.Data is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("Hubo un error tratando de obtener el horoscopo.")));
            return;
        }

        string horoscopo = horoscopeData.Data.HoroscopeData;
        DateTime date = DateTime.Parse(horoscopeData.Data.Date, CultureInfo.InvariantCulture);

        Random endMemberSignoDay = new((int)signo + date.DayOfYear + date.Year + (int)ctx.Member!.Id);

        (double rndAmor, double rndSalud, double rndDinero) =
            HoroscopoCalculator.Puntajes((int)signo, date.DayOfYear, date.Year, ctx.Member.Id);

        try
        {
            horoscopo = await TranslationHelper.TranslateAsync(client, horoscopo, "en", "es");
        }
        catch (Exception ex)
        {
            // Si el traductor no está disponible mostramos el texto original en inglés de la API.
            logger.LogWarning(ex, "No se pudo traducir el horoscopo, se muestra el texto original en inglés");
        }

        (string Texto, DiscordEmoji Emote) amor = FunService.GetHoroscopoCategoria(CategoriaHoroscopo.Amor, rndAmor, endMemberSignoDay);
        (string Texto, DiscordEmoji Emote) dinero = FunService.GetHoroscopoCategoria(CategoriaHoroscopo.Dinero, rndDinero, endMemberSignoDay);
        (string Texto, DiscordEmoji Emote) bienestar = FunService.GetHoroscopoCategoria(CategoriaHoroscopo.Bienestar, rndSalud, endMemberSignoDay);

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
            .WithThumbnail(ctx.Member.AvatarUrlPreferido());

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("confesion")]
    [Description("Confiesa algo")]
    public async Task Confesion(
        SlashCommandContext ctx,
        [Parameter("Confesion")] [Description("Lo que quieres confesar")] string confesion)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync(true);

        if (confessionsState.UserConfessed(ctx.User.Id))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("Ya confesaste hoy!")));
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
                        && (ctx.Guild.Id != config.GuildId || rangoRoles.RangoAPartirDe(ctx.Guild, x, RangoEnum.Tama, true)))
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
