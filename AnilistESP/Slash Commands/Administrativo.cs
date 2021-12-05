using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistESP
{
    [SlashRequireUserPermissions(Permissions.ManageGuild)]
    public class Administrativo : ApplicationCommandModule
    {
        private readonly FuncionesAuxiliares _funciones = new();

        [SlashCommand("yepmodetoggle", "Activa el Yep mode (Staff)")]
        public async Task ToggleYepMode(InteractionContext ctx, [Option("Emote", "El emote que quieres utilizar")] string emojiStr)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                DiscordEmoji emote = _funciones.ToEmoji(emojiStr);
                string name = emote.Name + "mode";
                if (emote != null)
                {
                    ServiciosSingleton service = ServiciosSingleton.GetServiciosSingleton();
                    if (!service.YepMode)
                    {
                        service.ActivarYepmpde(emote);
                        var builder = new DiscordInteractionResponseBuilder();
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{name} activado"
                        };
                        if (emote.Id != 0)
                        {
                            embed.WithImageUrl($"{emote.Url}");
                        }
                        builder.AddEmbed(embed);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                    }
                    else
                    {
                        service.ActivarYepmpde(emote);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Emote cambiado",
                            Description = $"El {name} ahora tiene asignado otro emote"
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
                        Description = "Debes pasar un emoji"
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
                    Description = "Comando no habilitado para este servidor"
                }));
            }
        }

        [SlashCommand("yepmodedisable", "Desactiva el Yep mode (Staff)")]
        public async Task DisableYepMode(InteractionContext ctx)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                ServiciosSingleton service = ServiciosSingleton.GetServiciosSingleton();
                string name = service.Emote.Name + "mode";
                if (service.YepMode)
                {
                    service.DesactivarYepMode();
                    var builder = new DiscordInteractionResponseBuilder();
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{name} desactivado",
                    };
                    builder.AddEmbed(embed);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Error",
                        Description = $"El {name} no estaba activado"
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
                    Description = "Comando no habilitado para este servidor"
                }));
            }
        }

        [SlashCommand("embed", "Manda un mensaje con embed")]
        public async Task Embed(InteractionContext ctx, [Option("Canal", "Donde se enviará el embed")] DiscordChannel canal)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.DeleteResponseAsync();

            var interactivity = ctx.Client.GetInteractivity();
            string customIdSelect = "select";

            var msgbuilder = new DiscordMessageBuilder();
            msgbuilder.AddEmbed(new DiscordEmbedBuilder { 
                Color = _funciones.GetColor()
            });

            msgbuilder.AddComponents(new DiscordSelectComponent(customIdSelect, "Selecciona una opción", new DiscordSelectComponentOption[] {
                new DiscordSelectComponentOption("Titulo", "Titulo", "El titulo del embed", false, null),
                new DiscordSelectComponentOption("Descripcion", "Descripcion", "La descripcion del embed", false, null),
                new DiscordSelectComponentOption("Imagen", "Imagen", "La imagen del embed", false, null),
                new DiscordSelectComponentOption("Footer", "Footer", "El footer del embed", false, null),
                new DiscordSelectComponentOption("Thumbnail", "Thumbnail", "El thumbnail del embed", false, null),
                new DiscordSelectComponentOption("URL", "URL", "URL del embed", false, null),
                new DiscordSelectComponentOption("PUBLICAR", "PUBLICAR", "Publica el embed", false, null),
                new DiscordSelectComponentOption("CANCELAR", "CANCELAR", "Cancela el embed", false, null),
            }));

            //var boton1 = new DiscordButtonComponent(ButtonStyle.Success, "publicar", "Publicar");
            //var boton2 = new DiscordButtonComponent(ButtonStyle.Danger, "cancelar", "Cancelar");
            //whbuilder.AddComponents(boton1, boton2);

            var msg = await msgbuilder.SendAsync(ctx.Channel);

            var response = await interactivity.WaitForSelectAsync(msg, ctx.User, customIdSelect, TimeSpan.FromMinutes(5));

            if (!response.TimedOut)
            {
                var result = response.Result;
                string idSeleccionado = result.Interaction.Data.Values[0];
                switch (idSeleccionado)
                {
                    case "Titulo":
                        //string titulo = await _funciones.GetStringInteractivity(ctx, "Escribe un titulo", "Titulo del embed", "Error", true);

                        break;
                }
            }

            await msg.DeleteAsync();

            DiscordEmbedBuilder builder = new();
            builder.WithColor(_funciones.GetColor());
        }
    }
}
