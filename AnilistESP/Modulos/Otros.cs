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
                try
                {
                    TimeZoneInfo spainZone, argentinaZone, chileZone, venezuelaZone, uruguayZone, costaricaZone, panamaZone, mexicoZone;
                    DateTime spainTime, argentinaTime, chileTime, venezuelaTime, uruguayTime, costaricaTime, panamaTime, mexicoTime;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        spainZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
                        spainTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, spainZone);

                        argentinaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
                        argentinaTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, argentinaZone);

                        chileZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
                        chileTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, chileZone);

                        venezuelaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Caracas");
                        venezuelaTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, venezuelaZone);

                        uruguayZone = TimeZoneInfo.FindSystemTimeZoneById("America/Montevideo");
                        uruguayTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, uruguayZone);

                        costaricaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica");
                        costaricaTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, costaricaZone);

                        panamaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Panama");
                        panamaTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, panamaZone);

                        mexicoZone = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
                        mexicoTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, mexicoZone);

                        string desc =
                        $"**España:**     {spainTime.ToString("dddd", CultureInfo.CreateSpecificCulture("es"))} {spainTime.Day} de {spainTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))} del {spainTime.Year} a las {spainTime.ToString("HH:mm")}\n" +
                        $"**Argentina:**  {argentinaTime.ToString("dddd", CultureInfo.CreateSpecificCulture("es"))} {argentinaTime.Day} de {argentinaTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))} del {argentinaTime.Year} a las {argentinaTime.ToString("HH:mm")}\n" +
                        $"**Chile:**      {chileTime.ToString("dddd", CultureInfo.CreateSpecificCulture("es"))} {chileTime.Day} de {chileTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))} del {chileTime.Year} a las {chileTime.ToString("HH:mm")}\n" +
                        $"**Venezuela:**  {venezuelaTime.ToString("dddd", CultureInfo.CreateSpecificCulture("es"))} {venezuelaTime.Day} de {venezuelaTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))} del {venezuelaTime.Year} a las {venezuelaTime.ToString("HH:mm")}\n" +
                        $"**Uruguay:**    {uruguayTime.ToString("dddd", CultureInfo.CreateSpecificCulture("es"))} {uruguayTime.Day} de {uruguayTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))} del {uruguayTime.Year} a las {uruguayTime.ToString("HH:mm")}\n" +
                        $"**Costa Rica:** {costaricaTime.ToString("dddd", CultureInfo.CreateSpecificCulture("es"))} {costaricaTime.Day} de {costaricaTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))} del {costaricaTime.Year} a las {costaricaTime.ToString("HH:mm")}\n" +
                        $"**Mexico:**     {mexicoTime.ToString("dddd", CultureInfo.CreateSpecificCulture("es"))} {mexicoTime.Day} de {mexicoTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))} del {mexicoTime.Year} a las {mexicoTime.ToString("HH:mm")}\n" +
                        $"**Panama:**     {panamaTime.ToString("dddd", CultureInfo.CreateSpecificCulture("es"))} {panamaTime.Day} de {panamaTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"))} del {panamaTime.Year} a las {panamaTime.ToString("HH:mm")}\n";

                        await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                        {
                            Title = "Horarios",
                            Description = desc,
                            Footer = funciones.GetFooter(ctx),
                            Color = funciones.GetColor()
                        });
                    }
                    else
                    {
                        ok = false;
                        error = "Comando no habilitado";
                    }
                }
                catch (TimeZoneNotFoundException)
                {
                    ok = false;
                    error = "No se ha podido encontrar una zona horaria";
                }
                catch (InvalidTimeZoneException)
                {
                    ok = false;
                    error = "Una zona horaria parece estar corrupta";
                }
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
