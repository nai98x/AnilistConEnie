using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace AnilistESP
{
    public class AniListConEnie_Bot
    {
        public DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public SlashCommandsExtension ApplicationCommands { get; private set; }

        private DiscordChannel LogChannel;

        private readonly FuncionesAuxiliares funciones = new();

        private bool Debug;

        public async Task RunAsync()
        {
            var json = string.Empty;
            using (var fs = File.OpenRead("config.json"))
            {
                using var sr = new StreamReader(fs, new UTF8Encoding(false));
                json = await sr.ReadToEndAsync().ConfigureAwait(false);
            }

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            string token, prefix;
            IDebuggingService mode = new DebuggingService();
            Debug = mode.RunningInDebugMode();
            if (Debug)
            {
                token = configJson.TokenTest;
                prefix = ConfigurationManager.AppSettings["PrefixTest"];
            }
            else
            {
                token = configJson.TokenProd;
                prefix = ConfigurationManager.AppSettings["PrefixProd"];
            }

            var Config = new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                ReconnectIndefinitely = true,
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.All
            };
            Client = new DiscordClient(Config);

            Client.Ready += OnClientReady;
            Client.ClientErrored += Client_ClientError;
            Client.Resumed += Client_Resumed;
            Client.GuildMemberRemoved += Client_GuildMemberRemoved;
            Client.ComponentInteractionCreated += Client_ComponentInteractionCreated;
            Client.MessageDeleted += Client_MessageDeleted;
            Client.MessageUpdated += Client_MessageUpdated;

            Client.UseInteractivity(new InteractivityConfiguration());

            ApplicationCommands = Client.UseSlashCommands();

            ApplicationCommands.SlashCommandExecuted += SlashCommands_SlashCommandExecuted;
            ApplicationCommands.SlashCommandErrored += SlashCommands_SlashCommandErrored;

            ApplicationCommands.ContextMenuExecuted += SlashCommands_ContextMenuExecuted;
            ApplicationCommands.ContextMenuErrored += SlashCommands_ContextMenuErrored;

            ulong pruebasBacklog = 853766076122005565;
            ulong guildProd = 862408834693070898;

            if (Debug)
            {
                ApplicationCommands.RegisterCommands<Anilist>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Usuarios>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Fun>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Roles>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Help>(pruebasBacklog);
            }
            else
            {
                ApplicationCommands.RegisterCommands<Anilist>(guildProd);
                ApplicationCommands.RegisterCommands<Usuarios>(guildProd);
                ApplicationCommands.RegisterCommands<Fun>(guildProd);
                ApplicationCommands.RegisterCommands<Roles>(guildProd);
                ApplicationCommands.RegisterCommands<Help>(guildProd);
            }

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                DmHelp = false,
                EnableDefaultHelp = false,
                CaseSensitive = false,
                IgnoreExtraArguments = true
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.RegisterCommands<Administracion>();

            Commands.RegisterConverter(new MemberConverter());

            await Client.ConnectAsync(new DiscordActivity { ActivityType = ActivityType.Playing, Name = prefix + "help" }, UserStatus.Online);

            var LogGuild = await Client.GetGuildAsync(862408834693070898);
            LogChannel = LogGuild.GetChannel(862410338577547324);

            await Task.Delay(-1);
        }

        private Task Client_MessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (e.Guild.Id == 862408834693070898 && e.Message.Channel.Id != LogChannel.Id && !e.Message.Author.IsBot)
                {
                    await LogChannel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Title = $"Mensaje editado en #{e.Message.Channel.Name}",
                        Color = DiscordColor.Yellow,
                        Author = new EmbedAuthor
                        {
                            IconUrl = e.Message.Author.AvatarUrl,
                            Name = $"{e.Message.Author.Username}#{e.Message.Author.Discriminator}"
                        }
                    }
                    .AddField("Antes:", e.MessageBefore.Content)
                    .AddField("Después:", e.Message.Content)
                    );
                }
            });
            return Task.CompletedTask;
        }

        private Task Client_MessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if(e.Guild.Id == 862408834693070898 && e.Message.Channel.Id != LogChannel.Id && !e.Message.Author.IsBot)
                {
                    await LogChannel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Title = $"Mensaje eliminado en #{e.Message.Channel.Name}",
                        Color = DiscordColor.Red,
                        Description = e.Message.Content,
                        Author = new EmbedAuthor
                        {
                            IconUrl = e.Message.Author.AvatarUrl,
                            Name = $"{e.Message.Author.Username}#{e.Message.Author.Discriminator}"
                        }
                    });
                }
            });
            return Task.CompletedTask;
        }

        private Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);


                if (e.Guild.Id == 862408834693070898)
                {
                    if (e.Interaction.Data.CustomId == "ReactionRolesColores" || e.Interaction.Data.CustomId == "ReactionRolesPaises")
                    {
                        foreach (var rolId in e.Interaction.Data.Values)
                        {
                            var rol = e.Guild.GetRole(ulong.Parse(rolId));
                            if (rol != null)
                            {
                                DiscordMember miembro = (DiscordMember)e.User;
                                try
                                {
                                    await miembro.GrantRoleAsync(rol);
                                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                                    {
                                        Content = $"Se te ha asignado el rol `{rol.Name}` exitosamente!",
                                        IsEphemeral = true
                                    });
                                    List<ulong> lista;
                                    if(e.Interaction.Data.CustomId == "ReactionRolesColores")
                                    {
                                        lista = funciones.IDRolesColoresAnilistEsp2();
                                    }
                                    else // Paises
                                    {
                                        lista = funciones.IDRolesPaisesAnilistEsp2();
                                    }
                                    var roles = miembro.Roles.ToList();
                                    foreach (var r in lista)
                                    {
                                        DiscordRole check = roles.Find(x => x.Id == r);
                                        if (r != rol.Id && check != null)
                                        {
                                            try
                                            {
                                                await miembro.RevokeRoleAsync(check);
                                            }
                                            catch (Exception exx)
                                            {
                                                await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                                                {
                                                    Content = $"Error asignando rol `{check.Name}`!",
                                                    IsEphemeral = true
                                                });
                                                await LogChannel.SendMessageAsync(new DiscordEmbedBuilder
                                                {
                                                    Color = DiscordColor.Red,
                                                    Title = $"Error asignando rol `{check.Name} (id: {check.Id})`",
                                                    Description = exx.Message
                                                });
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                                    {
                                        Content = $"Error asignando rol `{rol.Name}`!",
                                        IsEphemeral = true
                                    });
                                    await LogChannel.SendMessageAsync(new DiscordEmbedBuilder
                                    {
                                        Color = DiscordColor.Red,
                                        Title = $"Error asignando rol `{rol.Name} (id: {rol.Id})`",
                                        Description = ex.Message
                                    });
                                }
                            }
                        }
                    }
                }
            });
            return Task.CompletedTask;
        }

        private Task Client_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
        {
            e.Handled = true;
            _ = Task.Run(async () =>
            {
                UsuariosAnilist helper = new();
                var usuario = await helper.GetPerfil(e.Guild.Id, e.Member.Id);
                if(usuario != null)
                {
                    await funciones.BorrarMensajeUsuarioAnilist(sender, e.Guild, usuario.MessageId);
                    await helper.DeleteAnilist(e.Guild.Id, e.Member.Id);
                    await funciones.GrabarLogUsuarioOutAnilist(Client, e.Member, e.Guild);
                }
            });
            return Task.CompletedTask;
        }

        private Task OnClientReady(DiscordClient c, ReadyEventArgs e)
        {
            c.Logger.LogInformation("El cliente esta listo para procesar eventos.", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_Resumed(DiscordClient c, ReadyEventArgs e)
        {
            c.Logger.LogInformation("El cliente vuelve a estar listo para procesar eventos.", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_ClientError(DiscordClient c, ClientErrorEventArgs e)
        {
            e.Handled = true;
            _ = Task.Run(async () =>
            {
                if (e.Exception.Message != "An event handler caused the invocation of an asynchronous event to time out." &&
                e.Exception.Message != "One or more errors occurred. (Unauthorized: 403)")
                {
                    await LogChannel.SendMessageAsync(embed: new DiscordEmbedBuilder()
                    {
                        Title = "Ha ocurrido una excepcion",
                        Footer = new EmbedFooter()
                        {
                            Text = $"{DateTimeOffset.Now}"
                        },
                        Color = DiscordColor.Red
                    }.AddField("Tipo", $"{e.Exception.GetType()}", false)
                    .AddField("Descripcion", $"{e.Exception.Message}", false)
                    .AddField("Evento", $"{e.EventName}", false)
                    );
                }
            });
            return Task.CompletedTask;
        }

        private Task SlashCommands_SlashCommandExecuted(SlashCommandsExtension sender, SlashCommandExecutedEventArgs e)
        {
            e.Handled = true;
            _ = Task.Run(async () =>
            {
                await LogChannel.SendMessageAsync(funciones.LogInteractionCommand(e, "Slash Command ejecutado", true, false));
            });
            return Task.CompletedTask;
        }

        private Task SlashCommands_SlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
        {
            e.Handled = true;
            _ = Task.Run(async () =>
            {
                if (e.Exception is SlashExecutionChecksFailedException ex)
                {
                    await e.Context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    foreach (SlashCheckBaseAttribute check in ex.FailedChecks)
                    {
                        switch (check)
                        {
                            case SlashRequireOwnerAttribute:
                                await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Title = $"Acceso denegado",
                                    Description = $"Solo el dueño del bot puede ejecutar este comando",
                                    Color = DiscordColor.Red
                                }));
                                break;
                            case SlashRequireBotPermissionsAttribute bp:
                                await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Title = $"Acceso denegado",
                                    Description = $"El bot necesita el permiso `{bp.Permissions}` para ejecutar este comando",
                                    Color = DiscordColor.Red
                                }));
                                break;
                            case SlashRequireUserPermissionsAttribute up:
                                await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Title = $"Acceso denegado",
                                    Description = $"Necesitas el permiso `{up.Permissions}` para ejecutar este comando",
                                    Color = DiscordColor.Red
                                }));
                                break;
                            case SlashRequirePermissionsAttribute up:
                                await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Title = $"Acceso denegado",
                                    Description = $"Tu y el bot necesitan el permiso `{up.Permissions}` para ejecutar este comando",
                                    Color = DiscordColor.Red
                                }));
                                break;
                            case SlashRequireGuildAttribute:
                                await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Title = $"Servidor requerido",
                                    Description = $"Solo puedes ejecutar este comando dentro de un servidor",
                                    Color = DiscordColor.Red
                                }));
                                break;
                            case SlashRequireDirectMessageAttribute:
                                await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Title = $"DM requerido",
                                    Description = $"Solo puedes ejecutar este comando mandándome un mensaje privado",
                                    Color = DiscordColor.Red
                                }));
                                break;
                        }
                    }
                }
                else
                {
                    if (e.Exception is not NotFoundException)
                    {
                        await LogChannel.SendMessageAsync(funciones.LogInteractionCommand(e, "Error no controlado (Slash Commands)", true, true));
                    }
                }
            });
            return Task.CompletedTask;
        }

        private Task SlashCommands_ContextMenuExecuted(SlashCommandsExtension sender, ContextMenuExecutedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await LogChannel.SendMessageAsync(funciones.LogInteractionCommand(e, "Context Menu ejecutado", false, false));
            });
            return Task.CompletedTask;
        }

        private Task SlashCommands_ContextMenuErrored(SlashCommandsExtension sender, ContextMenuErrorEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await LogChannel.SendMessageAsync(funciones.LogInteractionCommand(e, "Error no controlado (Context Menus)", false, true));
            });
            return Task.CompletedTask;
        }
    }
}
