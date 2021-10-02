using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Usuarios : ApplicationCommandModule
    {
        private readonly FuncionesAuxiliares funciones = new();
        private readonly UsuariosDiscord usuariosService = new();

        [SlashCommand("birthdays", "Muestra los cumpleaños de los usuarios")]
        public async Task Birthdays(InteractionContext ctx, [Option("Mes", "Si quieres ver los cumpleaños del mes o todos los registrados")] bool month)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.DeleteResponseAsync();
            List<UserCumple> lista = await usuariosService.GetBirthdays((long)ctx.Guild.Id, month);
            string desc = string.Empty;
            var usuarios = await usuariosService.GetBirthdaysHoy((long)ctx.Guild.Id);
            if (usuarios.Count > 0)
            {
                desc += "**Cumplen años hoy:**\n";
                foreach (var user in usuarios)
                {
                    try
                    {
                        var miembro = await ctx.Guild.GetMemberAsync((ulong)user.Id);
                        if (user.MostrarYear ?? false)
                            desc += $"- **{miembro.Mention}** - Cumple **{DateTime.Now.Year - user.Birthday.Year} años**\n";
                        else
                            desc += $"- **{miembro.Mention}**\n";
                    }
                    catch (Exception) { }
                }
                desc += "\n";
            }

            if (!month)
            {
                desc += "**Cumplen años próximamente:**\n";
            }
            else
            {
                desc += "**Cumplen años en el próximo mes:**\n";
            }
            foreach (var user in lista)
            {
                try
                {
                    var miembro = await ctx.Guild.GetMemberAsync((ulong)user.Id);
                    int anios = DateTime.Now.Year - user.Birthday.Year;
                    if (DateTime.Now > new DateTime(day: user.Birthday.Day, month: user.Birthday.Month, year: DateTime.Now.Year))
                        anios += 1;
                    string dia = user.BirthdayActual.ToString("dddd", CultureInfo.CreateSpecificCulture("es"));
                    string mes = user.BirthdayActual.ToString("MMMM", CultureInfo.CreateSpecificCulture("es"));
                    if (user.MostrarYear ?? false)
                        desc += $"- **{miembro.Mention}** - Cumple **{anios} años** el {dia} {user.BirthdayActual.Day} de {mes} del {user.BirthdayActual.Year}\n";
                    else
                        desc += $"- **{miembro.Mention}** - Cumple el {dia} {user.BirthdayActual.Day} de {mes} del {user.BirthdayActual.Year}\n";
                }
                catch (Exception) { }
            }
            if (string.IsNullOrEmpty(desc))
            {
                desc = "(No hay ningún usuario registrado que cumpla años este mes)\n";
            }

            var embed = new DiscordEmbedBuilder
            {
                Footer = funciones.GetFooter(ctx),
                Color = funciones.GetColor(),
                Title = "Cumpleaños"
            };

            var interactivity = ctx.Client.GetInteractivity();
            var pages = interactivity.GeneratePagesInEmbed(desc, DSharpPlus.Interactivity.Enums.SplitType.Line, embed);
            _ = interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, token: new CancellationTokenSource(TimeSpan.FromSeconds(300)).Token).ConfigureAwait(false);
        }

        [SlashCommand("setbirthday", "Agrega o modifica tu cumpleaños")]
        public async Task SetBirthday(InteractionContext ctx, [Option("Day", "Dia")] double day, [Option("Month", "Mes")] double month, [Option("Year", "Año")] double year, [Option("Edad", "Si quieres mostrar tu edad")] bool mostrarEdad)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            DateTime fecha = new(day: (int)day, month: (int)month, year: (int)year);
            await usuariosService.SetBirthday(ctx.Guild.Id, ctx.User.Id, fecha, mostrarEdad);
            await ctx.EditResponseAsync(builder: new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder { 
                Title = "Cumpleaños registrado con éxito",
                Description = "Tu cumpleañus ha sido ingresado",
                Color = DiscordColor.Green
            }));
        }

        [SlashCommand("deletebirthday", "Elimina tu cumpleaños")]
        public async Task DeleteBirthday(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await usuariosService.DeleteBirthday(ctx);
            await ctx.EditResponseAsync(builder: new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Cumpleaños eliminado",
                Description = "Tu cumpleañus ha sido eliminado del servidor",
                Color = DiscordColor.Green
            }));
        }
    }
}
