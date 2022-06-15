using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AnilistConEnie.Commands
{
    public class Usuarios : ApplicationCommandModule
    {
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
                Footer = Funciones.GetFooter(ctx),
                Color = Funciones.GetColor(),
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
            await usuariosService.SetBirthday(ctx.User.Id, fecha, mostrarEdad);
            await ctx.EditResponseAsync(builder: new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
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

        [SlashCommand("silvia", "Actualiza la cuenta regresiva de Silvia")]
        [SlashRequireBotPermissions(Permissions.ManageNicknames)]
        public async Task Silvia(InteractionContext ctx)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                await ctx.DeferAsync();

                var cumple = new DateTimeOffset(day: 17, month: 9, year: 2022, hour: 0, minute: 0, second: 0, offset: TimeSpan.FromHours(1));
                if (DateTime.Now < new DateTime(day: 17, month: 9, year: 2022))
                {
                    var worrytap = await ctx.Guild.GetEmojiAsync(863085425282121818);
                    var cantidad = (DateTimeOffset.Now - cumple).TotalDays;
                    cantidad = Math.Abs(Math.Round(cantidad) - 1);
                    var silvia = await ctx.Guild.GetMemberAsync(392434346314825728);
                    await silvia.ModifyAsync(x => x.Nickname = $"{cantidad} dias");

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(
                    new DiscordEmbedBuilder
                    {
                        Title = "Dias actualizados",
                        Description = $"Solo faltan {cantidad} dias",
                        ImageUrl = worrytap.Url,
                        Color = DiscordColor.Green,
                    }));
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(
                    new DiscordEmbedBuilder
                    {
                        Title = "Demasiado tarde",
                        Description = "Ya pasaron los dias",
                        Color = DiscordColor.Red,
                    }));
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    IsEphemeral = true,
                }.AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = "Comando no habilitado para este servidor",
                }));
            }
        }
    }
}
