using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Model.Entities.Anilist;
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
    public static async Task<bool> GetSiNoInteractivity(CommandContext ctx, string titulo, string descripcion, DiscordEmbed? embed = null)
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

        if (result.TimedOut) return false;

        await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
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
    public static async Task SwitchTabsAsync(CommandContext ctx, Dictionary<string, DiscordEmbed> tabs)
    {
        if (tabs.Count is 0 or > 5) return;

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();

        // El token de la interacción expira a los 15 minutos (regla de Discord). Cortamos un poco
        // antes para alcanzar a editar el mensaje y dejar los botones deshabilitados mientras el
        // token sigue siendo válido.
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(14);
        TimeSpan inactivityTimeout = TimeSpan.FromMinutes(2);

        string activeTab = tabs.First().Key;

        while (true)
        {
            DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(tabs[activeTab])
                .AddActionRowComponent(BuildTabButtons(tabs.Keys, activeTab, disabled: false)));

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
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(tabs[activeTab])
            .AddActionRowComponent(BuildTabButtons(tabs.Keys, activeTab, disabled: true)));
    }

    private static IEnumerable<DiscordButtonComponent> BuildTabButtons(IEnumerable<string> keys, string activeTab, bool disabled) =>
        keys.Select(key => new DiscordButtonComponent(DiscordButtonStyle.Primary, key, key, disabled || key == activeTab)).ToList();

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
