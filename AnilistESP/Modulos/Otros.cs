using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Configuration;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Otros : BaseCommandModule
    {
        private readonly FuncionesAuxiliares funciones = new FuncionesAuxiliares();

        [Command("test"), Description("Testeos varios."), RequireOwner, Hidden]
        public async Task Test(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("uwu!");
        }

        [Command("horarios"), Aliases("recordatorios", "horario", "recordatorio"), Description("Horarios para diversos paises.")]
        public async Task Horarios(CommandContext ctx, [RemainingText]string texto)
        {
            bool fechaPuesta = DateTime.TryParse(texto, CultureInfo.CreateSpecificCulture("es-ES"), DateTimeStyles.None, out DateTime timeUtc);
            bool ok = true;
            string error = string.Empty;
            if (!fechaPuesta)
            {
                var interactivity = ctx.Client.GetInteractivity();
                var msgInicial = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Ingresa una fecha (En zona horaria UTC)",
                    Description = "En este formato: **dd/mm/yyyy**\n  Ejemplo: 30/01/2000 23:15"
                });
                var msgFechaInter = await interactivity.WaitForMessageAsync(xm => xm.Channel == ctx.Channel && xm.Author == ctx.User, TimeSpan.FromSeconds(60));
                if (!msgFechaInter.TimedOut)
                {
                    fechaPuesta = DateTime.TryParse(msgFechaInter.Result.Content, CultureInfo.CreateSpecificCulture("es-ES"), DateTimeStyles.None, out timeUtc);
                }
                if (msgFechaInter.Result != null)
                    await funciones.BorrarMensaje(ctx, msgFechaInter.Result.Id);
                if (msgInicial != null)
                    await funciones.BorrarMensaje(ctx, msgInicial.Id);
            }
            if (fechaPuesta)
            {
                await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Horarios",
                    Description = $"{Formatter.Timestamp(timeUtc, TimestampFormat.LongDateTime)}",
                    Footer = funciones.GetFooter(ctx),
                    Color = funciones.GetColor()
                });
            }
            else
            {
                ok = false;
                error = "Fecha mal escrita";
            }

            if (!ok)
            {
                var msgError = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = error,
                    Footer = funciones.GetFooter(ctx),
                    Color = funciones.GetColor()
                });
                await Task.Delay(5000);
                if (msgError != null)
                    await funciones.BorrarMensaje(ctx, msgError.Id);
            }
        }

        [Command("ping"), Description("Muestra el ping de Yumiko.")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
            {
                Title = "Latencia",
                Description = "🏓 Pong! `" + ctx.Client.Ping.ToString() + " ms" + "`",
                Footer = funciones.GetFooter(ctx),
                Color = funciones.GetColor()
            }).ConfigureAwait(false);
        }
    }
}
