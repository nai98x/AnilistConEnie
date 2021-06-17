using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Interactuar : BaseCommandModule
    {
        private readonly FuncionesAuxiliares funciones = new FuncionesAuxiliares();

        [Command("say"), Aliases("s"), Description("Yumiko habla en el chat."), RequireUserPermissions(DSharpPlus.Permissions.ManageGuild)]
        public async Task Say(CommandContext ctx, [Description("Mensaje para replicar")][RemainingText] string mensaje = null)
        {
            if (String.IsNullOrEmpty(mensaje))
            {
                var interactivity = ctx.Client.GetInteractivity();
                var msgAnime = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Escribe un mensaje",
                    Description = "Ejemplo: Hola! Soy Yumiko",
                    Footer = funciones.GetFooter(ctx),
                    Color = funciones.GetColor(),
                });
                var msgAnimeInter = await interactivity.WaitForMessageAsync(xm => xm.Channel == ctx.Channel && xm.Author == ctx.User, TimeSpan.FromSeconds(Convert.ToDouble(ConfigurationManager.AppSettings["TimeoutGeneral"])));
                if (!msgAnimeInter.TimedOut)
                {
                    mensaje = msgAnimeInter.Result.Content;
                    if (msgAnime != null)
                        await funciones.BorrarMensaje(ctx, msgAnime.Id);
                    if (msgAnimeInter.Result != null)
                        await funciones.BorrarMensaje(ctx, msgAnimeInter.Result.Id);
                }
                else
                {
                    var msgError = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                    {
                        Title = "Error",
                        Description = "Tiempo agotado esperando un mensaje",
                        Footer = funciones.GetFooter(ctx),
                        Color = DiscordColor.Red,
                    });
                    await Task.Delay(3000);
                    if (msgError != null)
                        await funciones.BorrarMensaje(ctx, msgError.Id);
                    if (msgAnime != null)
                        await funciones.BorrarMensaje(ctx, msgAnime.Id);
                    return;
                }
            }
            await ctx.Channel.SendMessageAsync(mensaje);
        }

        [Command("bestgirl"), Description("Best girl de Konosuba."), Cooldown(1, 300, CooldownBucketType.Guild)]
        public async Task BestGirl(CommandContext ctx)
        {
            Random rnd = new Random();
            int random = rnd.Next(3);
            switch (random)
            {
                case 0:
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                    {
                        Footer = funciones.GetFooter(ctx),
                        Color = DiscordColor.Cyan,
                        Title = "Best Girl (Konosuba)",
                        Description = "Aqua",
                        ImageUrl = "https://media.discordapp.net/attachments/816379048477065217/848254027639816202/b89362-ibkc0eoECaW1.png"
                    }).ConfigureAwait(false);
                    break;
                case 1:
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                    {
                        Footer = funciones.GetFooter(ctx),
                        Color = DiscordColor.Yellow,
                        Title = "Best Girl (Konosuba)",
                        Description = "Darkness",
                        ImageUrl = "https://media.discordapp.net/attachments/816379048477065217/848254149966561300/b89363-mm21Ll4NegUD.png"
                    }).ConfigureAwait(false);
                    break;
                case 2:
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                    {
                        Footer = funciones.GetFooter(ctx),
                        Color = DiscordColor.Red,
                        Title = "Best Girl (Konosuba)",
                        Description = "Megumin",
                        ImageUrl = "https://media.discordapp.net/attachments/816379048477065217/848254129084432428/b89361-x71P6YLrndd8.png"
                    }).ConfigureAwait(false);
                    break;
            }
        }

        [Command("ooc"), RequireNsfw]
        public async Task OOC(CommandContext ctx)
        {
            if (ctx.Guild.Id == 713809173573271613 || ctx.Guild.Id == 701813281718927441)
            {
                DiscordGuild discordOOC = await ctx.Client.GetGuildAsync(787033852258418768);
                if (discordOOC == null)
                {
                    await ctx.RespondAsync("Error al obtener servidor **AniList ESP OOC**").ConfigureAwait(false);
                    return;
                }
                DiscordChannel channel = discordOOC.GetChannel(787033979274264577);
                if (channel == null)
                {
                    await ctx.RespondAsync("Error al obtener canal **#capturas** del servidor **AniList ESP OOC**").ConfigureAwait(false);
                    return;
                }
                IReadOnlyList<DiscordMessage> mensajes = await channel.GetMessagesAsync();
                List<DiscordMessage> msgs = mensajes.ToList<DiscordMessage>();
                int cntMensajes = msgs.Count();
                DiscordMessage last = msgs.LastOrDefault();
                while (cntMensajes == 100)
                {
                    var mensajesAux = await channel.GetMessagesBeforeAsync(last.Id);

                    cntMensajes = mensajesAux.Count();
                    last = mensajesAux.LastOrDefault();

                    foreach (DiscordMessage mensaje in mensajesAux)
                    {
                        msgs.Add(mensaje);
                    }
                }
                List<Imagen> opciones = new List<Imagen>();
                foreach (DiscordMessage msg in msgs)
                {
                    var att = msg.Attachments.FirstOrDefault();
                    if (att != null && att.Url != null)
                    {
                        opciones.Add(new Imagen
                        {
                            Url = att.Url,
                            Autor = msg.Author
                        });
                    }
                }
                Random rnd = new Random();
                Imagen meme = opciones[rnd.Next(opciones.Count)];
                await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Footer = funciones.GetFooter(ctx),
                    Color = new DiscordColor(78, 63, 96),
                    Title = "Out of Context",
                    ImageUrl = $"{meme.Url}"
                }).ConfigureAwait(false);
                await ctx.Message.DeleteAsync();
            }
            else
            {
                var msg = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Comando no habilitado",
                    Description = $"Este comando no está disponible para este servidor",
                    Footer = funciones.GetFooter(ctx),
                    Color = funciones.GetColor()
                });
                await Task.Delay(5000);
                await funciones.BorrarMensaje(ctx, msg.Id);
            }
        }
    }
}
