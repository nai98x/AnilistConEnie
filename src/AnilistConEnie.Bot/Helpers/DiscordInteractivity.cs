using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Domain.Entities.Anilist;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>Helpers de interactividad (confirmaciones, selección y pestañas) sobre DSharpPlus.</summary>
public static class DiscordInteractivity
{
    /// <summary>
    /// Muestra un mensaje de confirmación (botones "Si"/"No") como followup y devuelve la respuesta del
    /// usuario. Devuelve <c>false</c> si se agota el tiempo de espera. Requiere que la interacción ya
    /// haya sido diferida.
    /// </summary>
    public static async Task<bool> GetSiNoInteractivity(CommandContext ctx, string titulo, string descripcion, DiscordEmbed? embed = null, bool borrarMensaje = false)
    {
        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();

        DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder()
            .AddEmbed(new DiscordEmbedBuilder { Title = titulo, Description = descripcion });

        if (embed != null) builder.AddEmbed(embed);

        builder.AddActionRowComponent(
            new DiscordButtonComponent(DiscordButtonStyle.Success, "true", "Si"),
            new DiscordButtonComponent(DiscordButtonStyle.Danger, "false", "No"));

        DiscordMessage msg = await ctx.FollowupAsync(builder);

        InteractivityResult<ComponentInteractionCreatedEventArgs> result =
            await interactivity.WaitForButtonAsync(msg, ctx.User, TimeSpan.FromMinutes(2));

        if (result.TimedOut)
        {
            if (borrarMensaje) await msg.DeleteAsync();
            return false;
        }

        await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        if (borrarMensaje) await msg.DeleteAsync();

        return bool.Parse(result.Result.Id);
    }

    public static async Task<int> GetElegidoAsync(CommandContext ctx, double timeoutGeneral, List<TitleDescription> opciones)
    {
        int cantidadOpciones = opciones.Count;
        if (cantidadOpciones == 1)
        {
            return 1;
        }

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        List<DiscordSelectComponentOption> options = [];
        const string customId = "dropdownGetElegido";

        int i = 0;
        opciones.ForEach(opc =>
        {
            if (i >= 25 || opc.Title == null) return;
            i++;
            options.Add(new DiscordSelectComponentOption(StringHelper.NormalizarBoton(opc.Title), $"{i}", opc.Description ?? string.Empty));
        });

        DiscordSelectComponent dropdown = new(customId, "Selecciona una opción", options);

        DiscordEmbedBuilder embed = new()
        {
            Color = DiscordColor.Blurple,
            Title = "Elige una opción",
        };

        DiscordMessage elegirMsg = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddActionRowComponent(dropdown).AddEmbed(embed));

        InteractivityResult<ComponentInteractionCreatedEventArgs> msgElegirInter = await interactivity.WaitForSelectAsync(elegirMsg, ctx.User, customId, TimeSpan.FromSeconds(timeoutGeneral));

        if (msgElegirInter.TimedOut) return -1;

        ComponentInteractionCreatedEventArgs resultElegir = msgElegirInter.Result;
        return int.Parse(resultElegir.Values[0]);
    }

    /// <summary>
    /// Muestra varios embeds (uno por pestaña) en un único mensaje, con un botón por pestaña para
    /// alternar entre ellos. La pestaña activa queda con su botón deshabilitado. Admite entre 1 y 5
    /// pestañas. Los botones se deshabilitan tras 2 minutos de inactividad o al acercarse al límite
    /// de vida de la interacción (15 minutos por regla de Discord), lo que ocurra primero.
    /// </summary>
    public static Task SwitchTabsAsync(CommandContext ctx, Dictionary<string, DiscordEmbed> tabs) =>
        SwitchTabsAsync(ctx, tabs.ToDictionary(kv => kv.Key, kv => new TabContent(kv.Value)));

    /// <summary>Contenido de una pestaña: el embed y, opcionalmente, una imagen para adjuntar como archivo.</summary>
    public readonly record struct TabContent(DiscordEmbed Embed, byte[]? Image = null, string? FileName = null);

    /// <summary>
    /// Igual que <see cref="SwitchTabsAsync(CommandContext, Dictionary{string, DiscordEmbed})"/> pero
    /// permite que cada pestaña adjunte además una imagen (ej: gráficos generados localmente), que se
    /// re-adjunta en cada edición del mensaje al cambiar de pestaña.
    /// </summary>
    public static async Task SwitchTabsAsync(CommandContext ctx, Dictionary<string, TabContent> tabs)
    {
        if (tabs.Count is 0 or > 5) return;

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();

        // El token de la interacción expira a los 15 minutos (regla de Discord). Cortamos un poco
        // antes para alcanzar a editar el mensaje y dejar los botones deshabilitados mientras el
        // token sigue siendo válido.
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(14);
        TimeSpan inactivityTimeout = TimeSpan.FromMinutes(2);

        string activeTab = tabs.First().Key;

        DiscordWebhookBuilder BuildTab(bool disabled)
        {
            TabContent tab = tabs[activeTab];
            DiscordWebhookBuilder builder = new DiscordWebhookBuilder()
                .AddEmbed(tab.Embed)
                .AddActionRowComponent(BuildTabButtons(tabs.Keys, activeTab, disabled));

            if (tab.Image is not null) builder.AddFile(tab.FileName!, tab.Image.ToMemoryStream());
            return builder;
        }

        while (true)
        {
            DiscordMessage message = await ctx.EditResponseAsync(BuildTab(disabled: false));

            TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            TimeSpan waitFor = remaining < inactivityTimeout ? remaining : inactivityTimeout;

            InteractivityResult<ComponentInteractionCreatedEventArgs> response =
                await interactivity.WaitForButtonAsync(message, ctx.User, waitFor);

            if (response.TimedOut) break;

            await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
            activeTab = response.Result.Id;
        }

        // Inactividad o límite de vida de la interacción alcanzado: dejamos la última pestaña con
        // todos los botones deshabilitados.
        await ctx.EditResponseAsync(BuildTab(disabled: true));
    }

    private static IEnumerable<DiscordButtonComponent> BuildTabButtons(IEnumerable<string> keys, string activeTab, bool disabled) =>
        keys.Select(key => new DiscordButtonComponent(DiscordButtonStyle.Primary, key, key, disabled || key == activeTab)).ToList();

    /// <summary>
    /// Parte <paramref name="texto"/> en páginas de a lo sumo <paramref name="lineasPorPagina"/> líneas,
    /// devolviendo un embed por página construido a partir de <paramref name="plantilla"/> (el mismo
    /// título/fields/imágenes en todas, description distinta por página, con footer "Página x/x" cuando
    /// hay más de una). A diferencia de <see cref="InteractivityExtension.GeneratePagesInEmbed"/> (que
    /// trae 15 líneas por página fijas, sin forma de configurarlo), esto permite elegir el tamaño.
    /// </summary>
    public static IReadOnlyList<DiscordEmbed> GenerarPaginasPorLineas(string texto, int lineasPorPagina, DiscordEmbedBuilder plantilla)
    {
        string[] lineas = texto.Split('\n');
        List<string> paginas = [];
        for (int i = 0; i < lineas.Length; i += lineasPorPagina)
            paginas.Add(string.Join('\n', lineas.Skip(i).Take(lineasPorPagina)));

        if (paginas.Count == 0) paginas.Add(string.Empty);

        DiscordEmbed baseEmbed = plantilla.Build();
        return paginas.Select((pagina, i) =>
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder(baseEmbed).WithDescription(pagina);
            if (paginas.Count > 1) embed.WithFooter($"Página {i + 1}/{paginas.Count}");
            return embed.Build();
        }).ToList();
    }

    /// <summary>
    /// Muestra distintas vistas (una por pestaña) como embed, cada una paginada de forma independiente
    /// con botones "Anterior"/"Siguiente" si no entra en una sola página (generar las páginas de cada
    /// pestaña con <see cref="GenerarPaginasPorLineas"/>). Los botones de pestaña solo se muestran si hay
    /// más de una; los de paginación solo si la pestaña activa tiene más de una página. Si hay una sola
    /// pestaña con una sola página, se muestra directo sin botones. Admite entre 1 y 3 pestañas (deja
    /// lugar para los 2 botones de paginación en la misma fila). Los botones se deshabilitan tras 2
    /// minutos de inactividad o al acercarse al límite de vida de la interacción (15 minutos por regla de
    /// Discord). Requiere que la respuesta ya haya sido diferida.
    /// </summary>
    public static async Task SwitchTabsPaginadoAsync(
        CommandContext ctx,
        IReadOnlyList<(string Id, string Label, IReadOnlyList<DiscordEmbed> Paginas)> tabs)
    {
        if (tabs.Count is 0 or > 3) return;

        string activeTab = tabs[0].Id;
        Dictionary<string, int> paginaPorTab = tabs.ToDictionary(t => t.Id, _ => 0);

        DiscordWebhookBuilder Build(bool botonesDeshabilitados)
        {
            (string Id, string Label, IReadOnlyList<DiscordEmbed> Paginas) tab = tabs.First(t => t.Id == activeTab);
            int totalPaginas = tab.Paginas.Count;
            DiscordEmbed embed = tab.Paginas[paginaPorTab[tab.Id]];

            DiscordWebhookBuilder builder = new DiscordWebhookBuilder().AddEmbed(embed);

            List<DiscordButtonComponent> botones = [];
            if (tabs.Count > 1)
                botones.AddRange(tabs.Select(t =>
                    new DiscordButtonComponent(DiscordButtonStyle.Primary, $"tab-{t.Id}", t.Label, botonesDeshabilitados || t.Id == activeTab)));

            if (totalPaginas > 1)
            {
                int pagina = paginaPorTab[tab.Id];
                botones.Add(new DiscordButtonComponent(DiscordButtonStyle.Secondary, "anterior", "Anterior", botonesDeshabilitados || pagina == 0));
                botones.Add(new DiscordButtonComponent(DiscordButtonStyle.Secondary, "siguiente", "Siguiente", botonesDeshabilitados || pagina == totalPaginas - 1));
            }

            if (botones.Count > 0) builder.AddActionRowComponent(botones);
            return builder;
        }

        if (tabs.Count == 1 && tabs[0].Paginas.Count == 1)
        {
            await ctx.EditResponseAsync(Build(botonesDeshabilitados: false));
            return;
        }

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();

        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(14);
        TimeSpan inactivityTimeout = TimeSpan.FromMinutes(2);

        while (true)
        {
            DiscordMessage message = await ctx.EditResponseAsync(Build(botonesDeshabilitados: false));

            TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            TimeSpan waitFor = remaining < inactivityTimeout ? remaining : inactivityTimeout;

            InteractivityResult<ComponentInteractionCreatedEventArgs> response =
                await interactivity.WaitForButtonAsync(message, ctx.User, waitFor);

            if (response.TimedOut) break;

            await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            string id = response.Result.Id;
            if (id.StartsWith("tab-", StringComparison.Ordinal))
            {
                activeTab = id["tab-".Length..];
            }
            else
            {
                int totalPaginas = tabs.First(t => t.Id == activeTab).Paginas.Count;
                int pagina = paginaPorTab[activeTab];
                paginaPorTab[activeTab] = id switch
                {
                    "anterior" => Math.Max(0, pagina - 1),
                    "siguiente" => Math.Min(totalPaginas - 1, pagina + 1),
                    _ => pagina
                };
            }
        }

        await ctx.EditResponseAsync(Build(botonesDeshabilitados: true));
    }

    /// <summary>
    /// Pagina una lista en un container de Components V2 con botones "Anterior"/"Siguiente". Cada item se
    /// renderiza con <paramref name="renderItem"/>; el header recibe una línea "Página x/x" cuando hay más
    /// de una página. "Anterior" se deshabilita en la primera página y "Siguiente" en la última; ambos se
    /// deshabilitan tras 3 minutos de inactividad o al acercarse al límite de vida de la interacción (15
    /// minutos por regla de Discord). Requiere que la respuesta ya haya sido diferida.
    /// </summary>
    public static async Task PaginarContainerV2Async<T>(
        CommandContext ctx,
        IReadOnlyList<T> items,
        int porPagina,
        string header,
        Func<T, DiscordComponent> renderItem,
        DiscordColor? color = null,
        bool separarItems = false)
    {
        // Discord limita a 40 componentes (anidados incluidos) por mensaje. Recortamos los items por
        // página al máximo que entra para no exceder el límite si el render de cada item es pesado.
        porPagina = AjustarPorPagina(items, porPagina, renderItem, separarItems);

        int totalPaginas = Math.Max(1, (int)Math.Ceiling(items.Count / (double)porPagina));
        int pagina = 0;

        DiscordWebhookBuilder Build(bool inactivo)
        {
            List<DiscordComponent> componentes =
            [
                new DiscordTextDisplayComponent(
                    header + (totalPaginas > 1 ? $"\n-# Página {pagina + 1}/{totalPaginas}" : "")),
                new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Large)
            ];

            bool primero = true;
            foreach (T item in items.Skip(pagina * porPagina).Take(porPagina))
            {
                if (separarItems && !primero)
                    componentes.Add(new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Small));
                componentes.Add(renderItem(item));
                primero = false;
            }

            DiscordWebhookBuilder builder = new DiscordWebhookBuilder()
                .EnableV2Components()
                .AddContainerComponent(new DiscordContainerComponent(componentes, color: color ?? DiscordColor.Blurple));

            if (totalPaginas > 1)
                builder.AddActionRowComponent(
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "anterior", "Anterior", inactivo || pagina == 0),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, "siguiente", "Siguiente", inactivo || pagina == totalPaginas - 1));

            return builder;
        }

        if (totalPaginas <= 1)
        {
            await ctx.EditResponseAsync(Build(inactivo: false));
            return;
        }

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();

        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(14);
        TimeSpan inactivityTimeout = TimeSpan.FromMinutes(3);

        while (true)
        {
            DiscordMessage message = await ctx.EditResponseAsync(Build(inactivo: false));

            TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            TimeSpan waitFor = remaining < inactivityTimeout ? remaining : inactivityTimeout;

            InteractivityResult<ComponentInteractionCreatedEventArgs> response =
                await interactivity.WaitForButtonAsync(message, ctx.User, waitFor);

            if (response.TimedOut) break;

            await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            pagina = response.Result.Id switch
            {
                "anterior" => Math.Max(0, pagina - 1),
                "siguiente" => Math.Min(totalPaginas - 1, pagina + 1),
                _ => pagina
            };
        }

        await ctx.EditResponseAsync(Build(inactivo: true));
    }

    private const int LimiteComponentesV2 = 40;

    private static int AjustarPorPagina<T>(IReadOnlyList<T> items, int porPagina, Func<T, DiscordComponent> renderItem, bool separarItems)
    {
        if (items.Count == 0) return Math.Max(1, porPagina);

        // Overhead fijo del container: el propio container, el text display del header, el separador y la
        // fila de botones (acción + Anterior + Siguiente) por si hay más de una página.
        const int overhead = 1 + 1 + 1 + 3;
        int costoItem = ContarComponentes(renderItem(items[0])) + (separarItems ? 1 : 0);

        int maximo = Math.Max(1, (LimiteComponentesV2 - overhead) / costoItem);
        return Math.Min(porPagina, maximo);
    }

    private static int ContarComponentes(DiscordComponent componente) => componente switch
    {
        DiscordContainerComponent container => 1 + container.Components.Sum(ContarComponentes),
        DiscordActionRowComponent row => 1 + row.Components.Sum(ContarComponentes),
        DiscordSectionComponent section => 1 + section.Components.Sum(ContarComponentes)
                                             + (section.Accessory is null ? 0 : ContarComponentes(section.Accessory)),
        _ => 1
    };
}
