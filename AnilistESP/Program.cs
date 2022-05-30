namespace AnilistESP
{
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
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
    using System.Configuration;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using static DSharpPlus.Entities.DiscordEmbedBuilder;

    class Program
    {
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static SlashCommandsExtension ApplicationCommands { get; private set; }

        private static DiscordChannel LogChannel;

        private static bool Debug;

        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            var json = string.Empty;
            using (var fs = File.OpenRead("config.json"))
            {
                using var sr = new StreamReader(fs, new UTF8Encoding(false));
                json = await sr.ReadToEndAsync().ConfigureAwait(false);
            }

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            IDebuggingService mode = new DebuggingService();
            Debug = mode.RunningInDebugMode();

            var Config = new DiscordConfiguration
            {
                Token = Debug ? configJson.TokenTest : configJson.TokenProd,
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
            Client.MessageCreated += Client_MessageCreated;
            Client.MessageReactionAdded += Client_MessageReactionAdded;
            //Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            Client.GuildDownloadCompleted += Client_GuildDownloadCompleted;

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
                ApplicationCommands.RegisterCommands<Intercambios>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<IntercambiosAdmin>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Administrativo>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Help>(pruebasBacklog);
            }
            else
            {
                ApplicationCommands.RegisterCommands<Anilist>(guildProd);
                ApplicationCommands.RegisterCommands<Usuarios>(guildProd);
                ApplicationCommands.RegisterCommands<Fun>(guildProd);
                ApplicationCommands.RegisterCommands<Roles>(guildProd);
                ApplicationCommands.RegisterCommands<Intercambios>(guildProd);
                ApplicationCommands.RegisterCommands<IntercambiosAdmin>(guildProd);
                ApplicationCommands.RegisterCommands<Administrativo>(guildProd);
                ApplicationCommands.RegisterCommands<Help>(guildProd);
            }

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { Debug ? ConfigurationManager.AppSettings["PrefixTest"] : ConfigurationManager.AppSettings["PrefixProd"] },
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

            await Client.ConnectAsync(new DiscordActivity { ActivityType = ActivityType.Playing, Name = "/help" }, UserStatus.Online);

            DiscordGuild LogGuild;
            if (!Debug)
            {
                LogGuild = await Client.GetGuildAsync(862408834693070898);
                LogChannel = LogGuild.GetChannel(862410338577547324);
            }
            else
            {
                LogGuild = await Client.GetGuildAsync(853766076122005565);
                LogChannel = LogGuild.GetChannel(891840653162582087);
            }

            await Task.Delay(-1);
        }

        private static Task Client_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                var service = new UsuariosAnilist();
                ServiciosSingleton servicio = ServiciosSingleton.GetServiciosSingleton();
                var usuarios = await service.GetPerfilesServidor(862408834693070898);
                servicio.SetUsuarios(usuarios);
            });

            return Task.CompletedTask;
        }

        private static Task Client_MessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (e.Guild.Id == 862408834693070898 && e.Channel.Id == 862486026937040916) // Sugerencias Añilist
                {
                    DiscordEmoji yes = DiscordEmoji.FromUnicode(sender, "✅");
                    DiscordEmoji no = DiscordEmoji.FromUnicode(sender, "❌");

                    if (!e.Emoji.Equals(yes) && !e.Emoji.Equals(no))
                    {
                        await e.Message.DeleteReactionAsync(e.Emoji, e.User);
                    }
                }
            });
            return Task.CompletedTask;
        }

        private static Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            ServiciosSingleton service = ServiciosSingleton.GetServiciosSingleton();

            bool yepmode = service.YepMode;

            _ = Task.Run(async () =>
            {
                if (e.Guild.Id == 862408834693070898 && e.Channel.Id == 862408834693070901)
                {
                    if (yepmode)
                    {
                        DiscordEmoji emoji = service.Emote;
                        if (emoji != null)
                        {
                            await e.Message.CreateReactionAsync(emoji);
                        }
                    }
                }
            });
            return Task.CompletedTask;
        }

        private static Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            });
            return Task.CompletedTask;
        }

        private static Task Client_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
        {
            e.Handled = true;
            _ = Task.Run(async () =>
            {
                UsuariosAnilist helper = new();
                var usuario = await helper.GetPerfil(e.Guild.Id, e.Member.Id);
                if (usuario != null)
                {
                    await Funciones.BorrarMensajeUsuarioAnilist(sender, e.Guild, usuario.MessageId);
                    await helper.DeleteAnilist(e.Guild.Id, e.Member.Id);
                    await Funciones.GrabarLogUsuarioOutAnilist(Client, e.Member, e.Guild);
                }
            });
            return Task.CompletedTask;
        }

        private static Task OnClientReady(DiscordClient c, ReadyEventArgs e)
        {
            c.Logger.LogInformation("El cliente esta listo para procesar eventos.", DateTime.Now);
            return Task.CompletedTask;
        }

        private static Task Client_Resumed(DiscordClient c, ReadyEventArgs e)
        {
            c.Logger.LogInformation("El cliente vuelve a estar listo para procesar eventos.", DateTime.Now);
            return Task.CompletedTask;
        }

        private static Task Client_ClientError(DiscordClient c, ClientErrorEventArgs e)
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

        private static Task SlashCommands_SlashCommandExecuted(SlashCommandsExtension sender, SlashCommandExecutedEventArgs e)
        {
            e.Handled = true;
            _ = Task.Run(async () =>
            {
                await LogChannel.SendMessageAsync(Funciones.LogInteractionCommand(e, "Slash Command ejecutado", true, false));
            });
            return Task.CompletedTask;
        }

        private static Task SlashCommands_SlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
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
                        await LogChannel.SendMessageAsync(Funciones.LogInteractionCommand(e, "Error no controlado (Slash Commands)", true, true));
                    }
                }
            });
            return Task.CompletedTask;
        }

        private static Task SlashCommands_ContextMenuExecuted(SlashCommandsExtension sender, ContextMenuExecutedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await LogChannel.SendMessageAsync(Funciones.LogInteractionCommand(e, "Context Menu ejecutado", false, false));
            });
            return Task.CompletedTask;
        }

        private static Task SlashCommands_ContextMenuErrored(SlashCommandsExtension sender, ContextMenuErrorEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await LogChannel.SendMessageAsync(Funciones.LogInteractionCommand(e, "Error no controlado (Context Menus)", false, true));
            });
            return Task.CompletedTask;
        }
    }
}
