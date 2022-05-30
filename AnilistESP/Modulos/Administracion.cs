using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnilistESP
{
    [RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
    public class Administracion : BaseCommandModule
    {
        [Command("say"), Aliases("s"), Description("Yumiko habla en el chat.")]
        public async Task Say(CommandContext ctx, [Description("Mensaje para replicar")][RemainingText] string mensaje = null)
        {
            var interactivty = ctx.Client.GetInteractivity();

            bool usarEmbed = await Funciones.GetSiNoInteractivity(ctx, interactivty, "Usar embed", "Determina si se mandará un embed o un mensaje normal");
            if (usarEmbed)
            {
                DiscordEmbedBuilder embed = await Funciones.CrearEmbed(ctx, interactivty);
                if (embed != null)
                {
                    await ctx.Channel.SendMessageAsync(embed);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(mensaje))
                {
                    await ctx.Channel.SendMessageAsync(mensaje);
                }
            }
        }

        [Command("paises"), Aliases("arrpaises"), Description("Agrega reaction roles de paises."), Hidden, RequireOwner]
        public async Task AddReactionRolesPaises(CommandContext ctx, DiscordChannel canal)
        {
            DiscordEmbedBuilder embed = await Funciones.CrearEmbed(ctx, ctx.Client.GetInteractivity());

            await canal.SendMessageAsync(new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(new DiscordSelectComponent("ReactionRolesPaises", "Selecciona tu país", new DiscordSelectComponentOption[]
                {
                    new DiscordSelectComponentOption("Argentina", "863687575331012618", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_ar:"))),
                    new DiscordSelectComponentOption("Bolivia", "863688124696625153", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_bo:"))),
                    new DiscordSelectComponentOption("Chile", "863687136543899658", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_cl:"))),
                    new DiscordSelectComponentOption("Colombia", "863687047842889748", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_co:"))),
                    new DiscordSelectComponentOption("Costa Rica", "863686892549570560", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_cr:"))),
                    new DiscordSelectComponentOption("Cuba", "863688054279766068", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_cu:"))),
                    new DiscordSelectComponentOption("Ecuador", "863687910997229591", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_ec:"))),
                    new DiscordSelectComponentOption("El Salvador", "863687219448643584", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_sv:"))),
                    new DiscordSelectComponentOption("España", "863687790762655794", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_es:"))),
                    new DiscordSelectComponentOption("Guatemala", "863687501734215692", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_gu:"))),
                    new DiscordSelectComponentOption("Honduras", "863688360178876416", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_hn:"))),
                    new DiscordSelectComponentOption("México", "863687990349135882", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_mx:"))),
                    new DiscordSelectComponentOption("Nicaragua", "863688263122681877", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_ni:"))),
                    new DiscordSelectComponentOption("Panama", "863688518081314846", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_pa:"))),
                    new DiscordSelectComponentOption("Paraguay", "863687727065202708", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_py:"))),
                    new DiscordSelectComponentOption("Peru", "863688438842785812", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_pe:"))),
                    new DiscordSelectComponentOption("Puerto Rico", "863687661150797844", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_pr:"))),
                    new DiscordSelectComponentOption("Rep. Dominicana", "863688589572702208", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_do:"))),
                    new DiscordSelectComponentOption("Uruguay", "863687410881265674", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_uy:"))),
                    new DiscordSelectComponentOption("Venezuela", "863687332880580609", null, false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":flag_ve:"))),
                }, false, 1, 1))
            );
        }

        [Command("addreactionroles"), Aliases("arr"), Description("Agrega reaction roles."), Hidden, RequireOwner]
        public async Task AddReactionRoles(CommandContext ctx, DiscordChannel canal, int opcionesMinimas, int opcionesMaximas, params DiscordRole[] roles)
        {
            var interactivty = ctx.Client.GetInteractivity();
            string error = string.Empty;

            string customId, placeholder;
            if (await Funciones.GetSiNoInteractivity(ctx, interactivty, "¿Vas a agregar colores?", "Para funcionamiento interno"))
            {
                customId = "ReactionRolesColores";
                placeholder = "Selecciona un color";
            }
            else
            {
                customId = "ReactionRoles";
                placeholder = "Selecciona una opción";
            }

            List<DiscordSelectComponentOption> opciones = new();
            foreach (var rol in roles)
            {
                opciones.Add(new(label: $"{rol.Name}", value: $"{rol.Id}"));
            }

            DiscordEmbedBuilder embed = await Funciones.CrearEmbed(ctx, interactivty);
            if (embed != null)
            {
                await canal.SendMessageAsync(new DiscordMessageBuilder()
                .AddComponents(new DiscordSelectComponent(customId, placeholder, opciones, false, opcionesMinimas, opcionesMaximas))
                .AddEmbed(embed));
            }
            else
            {
                error = "No se ha podido crear el embed correctamente";
            }

            if (!string.IsNullOrEmpty(error))
            {
                var msg = await canal.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = error,
                    Color = DiscordColor.Red,
                    Footer = Funciones.GetFooter(ctx)
                });
                await Task.Delay(5000);
                await Funciones.BorrarMensaje(ctx, msg.Id);
            }
        }
    }
}
