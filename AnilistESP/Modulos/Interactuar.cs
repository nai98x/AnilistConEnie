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

        [Command("bestgirl"), Description("Best girl de Konosuba."), Cooldown(1, 300, CooldownBucketType.Guild)]
        public async Task BestGirl(CommandContext ctx)
        {
            if(ctx.Guild.Id == 701813281718927441)
            {
                DiscordRole rol;
                Random rnd = new Random();
                int random = rnd.Next(4);
                switch (random)
                {
                    case 0:
                        rol = ctx.Guild.GetRole(725562290148999240);
                        await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                        {
                            Footer = funciones.GetFooter(ctx),
                            Color = DiscordColor.Cyan,
                            Title = "Aqua",
                            Description = $"¡Ahora soy {rol.Mention}!",
                            ImageUrl = "https://media.discordapp.net/attachments/816379048477065217/848254027639816202/b89362-ibkc0eoECaW1.png"
                        }).ConfigureAwait(false);
                        break;
                    case 1:
                        rol = ctx.Guild.GetRole(725425507797303409);
                        await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                        {
                            Footer = funciones.GetFooter(ctx),
                            Color = DiscordColor.Yellow,
                            Title = "Darkness",
                            Description = $"¡Ahora soy {rol.Mention}!",
                            ImageUrl = "https://media.discordapp.net/attachments/816379048477065217/848254149966561300/b89363-mm21Ll4NegUD.png"
                        }).ConfigureAwait(false);
                        break;
                    case 2:
                        rol = ctx.Guild.GetRole(718187374953103389);
                        await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                        {
                            Footer = funciones.GetFooter(ctx),
                            Color = DiscordColor.Red,
                            Title = "Megumin",
                            Description = $"¡Ahora soy {rol.Mention}!",
                            ImageUrl = "https://media.discordapp.net/attachments/816379048477065217/848254129084432428/b89361-x71P6YLrndd8.png"
                        }).ConfigureAwait(false);
                        break;
                    case 3:
                        rol = ctx.Guild.GetRole(859207787703304222);
                        await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                        {
                            Footer = funciones.GetFooter(ctx),
                            Color = DiscordColor.Green,
                            Title = "Kazuma",
                            Description = $"¡Ahora soy {rol.Mention}!",
                            ImageUrl = "https://media.discordapp.net/attachments/854384291734356009/859576198748176453/b89364-7Th8Tv1XKJtt.png"
                        }).ConfigureAwait(false);
                        break;
                }
            }
            else
            {
                var msg = await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder
                {
                    Title = "Comando no habilitado",
                    Description = $"Este comando no puede ser ejecutado en este servidor",
                    Footer = funciones.GetFooter(ctx),
                    Color = funciones.GetColor()
                });
                await Task.Delay(5000);
                await funciones.BorrarMensaje(ctx, msg.Id);
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
