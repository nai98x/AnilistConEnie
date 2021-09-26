using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Fun : ApplicationCommandModule
    {
        private readonly FuncionesAuxiliares funciones = new();

        [SlashCommand("ship", "Elegir la ship de un usuario")]
        public async Task Ship(InteractionContext ctx, [Option("Usuario", "El usuario del que quieres ver su ship")] DiscordUser usuario = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            usuario ??= ctx.User;
            DiscordMember member = (DiscordMember)usuario;

            Random rnd = new();
            DiscordMember elegido;

            var miembros = ctx.Guild.Members.Where(x => x.Value.IsBot == false && x.Value.Id != usuario.Id);
            elegido = miembros.ElementAt(rnd.Next(miembros.Count() - 1)).Value;

            string shipeoUsr;
            DiscordMember ctxMiembro = await ctx.Guild.GetMemberAsync(usuario.Id);
            shipeoUsr = ctxMiembro.DisplayName;

            string avatar1 = usuario.GetAvatarUrl(ImageFormat.Png, 128);
            string avatar2 = elegido.GetAvatarUrl(ImageFormat.Png, 128);
            var imagen = funciones.MergeImage(avatar1, avatar2);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Shippeo",
                Description = $"Shippeo a {ctxMiembro.Mention} con **{elegido.Mention}** 💘",
                ImageUrl = "attachment://imagen.png"
            }).AddFile("imagen.png", funciones.ToStream(imagen)));
        }

        [SlashCommand("ooc", "Out of Context")]
        public async Task OOC(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            if (ctx.Guild.Id == 862408834693070898 || ctx.Guild.Id == 853766076122005565)
            {
                DiscordGuild discordOOC = await ctx.Client.GetGuildAsync(787033852258418768);
                if (discordOOC == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error al obtener servidor **Añilist Extra**"));
                    return;
                }
                DiscordChannel channel = discordOOC.GetChannel(787033979274264577);
                if (channel == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error al obtener canal **#capturas** del servidor **Añilist Extra**"));
                    return;
                }
                IReadOnlyList<DiscordMessage> mensajes = await channel.GetMessagesAsync();
                List<DiscordMessage> msgs = mensajes.ToList<DiscordMessage>();
                int cntMensajes = msgs.Count;
                DiscordMessage last = msgs.LastOrDefault();
                while (cntMensajes == 100)
                {
                    var mensajesAux = await channel.GetMessagesBeforeAsync(last.Id);

                    cntMensajes = mensajesAux.Count;
                    last = mensajesAux.LastOrDefault();

                    foreach (DiscordMessage mensaje in mensajesAux)
                    {
                        msgs.Add(mensaje);
                    }
                }
                List<Imagen> opciones = new();
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
                Random rnd = new();
                Imagen meme = opciones[rnd.Next(opciones.Count)];
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Blurple,
                    Title = "Out of Context",
                    ImageUrl = $"{meme.Url}"
                }));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Comando no habilitado",
                    Description = $"Este comando no está disponible para este servidor",
                    Color = DiscordColor.Red
                }));
            }
        }
    }
}
