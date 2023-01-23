namespace AnilistESP
{
    using AnilistConEnie.Commands;
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
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using static DSharpPlus.Entities.DiscordEmbedBuilder;

    class Program
    {
        public static DiscordClient Client { get; private set; }
        public static SlashCommandsExtension ApplicationCommands { get; private set; }
        public static CommandsNextExtension TextCommands { get; private set; }

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
                Intents = DiscordIntents.All,
                LogUnknownEvents = false
            };
            Client = new DiscordClient(Config);

            Client.Ready += OnClientReady;
            Client.ClientErrored += Client_ClientError;
            Client.Resumed += Client_Resumed;
            Client.GuildMemberRemoved += Client_GuildMemberRemoved;
            Client.ComponentInteractionCreated += Client_ComponentInteractionCreated;
            Client.MessageCreated += Client_MessageCreated;
            Client.MessageReactionAdded += Client_MessageReactionAdded;
            Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            //Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            Client.GuildDownloadCompleted += Client_GuildDownloadCompleted;
            Client.VoiceStateUpdated += Client_VoiceStateUpdated;

            Client.UseInteractivity(new InteractivityConfiguration());

            ApplicationCommands = Client.UseSlashCommands();

            ApplicationCommands.SlashCommandExecuted += SlashCommands_SlashCommandExecuted;
            ApplicationCommands.SlashCommandErrored += SlashCommands_SlashCommandErrored;

            ApplicationCommands.ContextMenuExecuted += SlashCommands_ContextMenuExecuted;
            ApplicationCommands.ContextMenuErrored += SlashCommands_ContextMenuErrored;

            ulong pruebasBacklog = 862408834693070898;
            ulong guildProd = 862408834693070898;

            if (Debug)
            {
                ApplicationCommands.RegisterCommands<Anilist>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Usuarios>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Fun>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Challenges>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Intercambios>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<IntercambiosAdmin>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Highlights>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Tatsu>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Administrativo>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Help>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Owner>(pruebasBacklog);
            }
            else
            {
                ApplicationCommands.RegisterCommands<Anilist>(guildProd);
                ApplicationCommands.RegisterCommands<Usuarios>(guildProd);
                ApplicationCommands.RegisterCommands<Fun>(guildProd);
                ApplicationCommands.RegisterCommands<Challenges>(guildProd);
                ApplicationCommands.RegisterCommands<Intercambios>(guildProd);
                ApplicationCommands.RegisterCommands<IntercambiosAdmin>(guildProd);
                ApplicationCommands.RegisterCommands<Highlights>(guildProd);
                ApplicationCommands.RegisterCommands<Tatsu>(guildProd);
                ApplicationCommands.RegisterCommands<Administrativo>(guildProd);
                ApplicationCommands.RegisterCommands<Help>(guildProd);
                ApplicationCommands.RegisterCommands<Owner>(guildProd);
            }

            TextCommands = Client.UseCommandsNext(new CommandsNextConfiguration()
            {
                EnableDefaultHelp = false,
                IgnoreExtraArguments = true,
                EnableDms = false,
                StringPrefixes = new[] { "a!" },
                EnableMentionPrefix = true
            });

            TextCommands.CommandErrored += TextCommands_CommandErrored;

            TextCommands.RegisterCommands<Emojis>();

            await Client.ConnectAsync(new DiscordActivity { ActivityType = ActivityType.Playing, Name = "/help" }, UserStatus.Online);

            DiscordGuild LogGuild;
            if (!Debug)
            {
                LogGuild = await Client.GetGuildAsync(862408834693070898);
                LogChannel = LogGuild.GetChannel(862410338577547324);
            }
            else
            {
                LogGuild = await Client.GetGuildAsync(862408834693070898);
                LogChannel = LogGuild.GetChannel(862410338577547324);
            }

            await Task.Delay(-1);
        }

        private static Task Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs e)
        {
            _ = Task.Run(async () => { 
                if (e.Guild.Id == 862408834693070898)
                {
                    if (e.RolesAfter != null && e.RolesBefore != null)
                    {
                        var distinctRoles = e.RolesAfter.Intersect(e.RolesBefore);
                        if (distinctRoles.Count() == 1 && distinctRoles.First().Id == 863525246404263976)
                        {
                            try
                            {
                                DiscordRole coloresExtra = e.Guild.Roles[1034191638714650736];
                                if (!e.Member.Roles.Any(x => x.Id == coloresExtra.Id))
                                {
                                    await e.Member.GrantRoleAsync(coloresExtra);
                                }
                            } catch(Exception) { };
                        }
                    }
                }
            });

            return Task.CompletedTask;
        }

        private static Task Client_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            _ = Task.Run(async () =>
            {

                ServiciosSingleton servicio = ServiciosSingleton.GetServiciosSingleton();

                var userService = new UsuariosAnilist();
                var usuarios = await userService.GetListaUsuarios();
                servicio.SetUsuarios(usuarios);
                sender.Logger.LogInformation("Usuarios de AniList cargados");

                var highlightService = new HighlightsDAL();
                var highlights = await highlightService.GetListaHighlights();
                servicio.SetHighlightedWords(highlights);
                sender.Logger.LogInformation("Highlights cargadas");

                try
                {
                    var guild = sender.Guilds[862408834693070898];
                    ulong senpai = 863525246404263976;
                    ulong hikikomori = 863525128403025961;
                    ulong sensei = 863524938954571816;
                    ulong ousama = 966815478507012106;
                    ulong teiou = 966815813078224907;
                    DiscordRole coloresExtra = guild.Roles[1034191638714650736];

                    guild.Members.ToList().ForEach(async member =>
                    {
                        if (member.Value.Roles.Any(x => x.Id == senpai) || member.Value.Roles.Any(x => x.Id == hikikomori) || member.Value.Roles.Any(x => x.Id == sensei) || 
                            member.Value.Roles.Any(x => x.Id == ousama) || member.Value.Roles.Any(x => x.Id == teiou))
                        {
                            if (!member.Value.Roles.Any(x => x.Id == coloresExtra.Id)) // Colores extra
                            {
                                await member.Value.GrantRoleAsync(coloresExtra);
                            }
                        }
                    });

                } catch (Exception) { } /* Nothing to do */
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
                if (e.Guild != null && e.Guild.Id == 862408834693070898)
                {
                    if (yepmode && e.Channel.Id == 862408834693070901)
                    {
                        DiscordEmoji emoji = service.Emote;
                        if (emoji != null)
                        {
                            await e.Message.CreateReactionAsync(emoji);
                        }
                    }
                }

                #region Highlights
                var words = service.GetHighlightedWords();
                if (words != null && words.Count > 0)
                {
                    var textSplit = e.Message.Content.Split(" ").ToList();
                    var intersect = textSplit.Where(x => words.Values.Any(d => d.Contains(x))).ToList();
                    if (intersect.Any())
                    {
                        foreach (var word in intersect)
                        {
                            var targets = words.Where(x => x.Value.Contains(word)).Select(y => y.Key).Where(u => u != e.Message.Author.Id).ToList();

                            foreach (var target in targets)
                            {
                                try
                                {
                                    var member = e.Guild.Members[target];
                                    if (member != null)
                                    {
                                        var dmChannel = await member.CreateDmChannelAsync();

                                        string mentionedMessage = $"Fuiste mencionado en {e.Channel.Mention} con la palabra: {Formatter.Bold(intersect.First())}";
                                        string message = $"[{Formatter.Timestamp(e.Message.CreationTimestamp, TimestampFormat.LongTime)}] {e.Author.Username}#{e.Author.Discriminator}: {e.Message.Content}";

                                        await dmChannel.SendMessageAsync(mentionedMessage, new DiscordEmbedBuilder
                                        {
                                            Title = intersect.First(),
                                            Description = Funciones.NormalizarDescription(message),
                                            ImageUrl = "https://images-ext-1.discordapp.net/external/uCEpGlkbms8IptErmq3l0lANEWFhtcfEXylXxlMm3VA/https/cdn.discordapp.com/emojis/867893834921803826.gif",
                                            Color = DiscordColor.Cyan
                                        }.AddField("Mensaje", Formatter.MaskedUrl("Ir", e.Message.JumpLink)));
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                }
                #endregion
            });
            return Task.CompletedTask;
        }

        private static Task Client_VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            var singleton = ServiciosSingleton.GetServiciosSingleton();
            ulong prodGuildId = 862408834693070898;
            ulong testGuildId = 853766076122005565;

            if (e.Guild.Id == prodGuildId || e.Guild.Id == testGuildId)
            {
                if (e.Before?.Channel?.Id != null)
                {
                    if (singleton.EsCanalTemporal(e.Before.Channel.Id) && e.Before.Channel.Users.Count == 0) // Borro el canal anterior si era temporal
                    {
                        _ = Task.Run(async () =>
                        {
                            singleton.EliminarCanalTemporal(e.Before.Channel.Id);
                            await e.Guild.GetChannel(e.Before.Channel.Id).DeleteAsync();
                        });
                    }
                }

                if (e.After?.Channel?.Id != null)
                {
                    ulong parentChannelId = (e.Guild.Id == prodGuildId) ? 862408834693070900 : (ulong)853766076122005567;
                    if (e.After.Channel.ParentId == parentChannelId)
                    {
                        ulong channelCreatorId = (e.Guild.Id == prodGuildId) ? 866057800093007903 : (ulong)891842909622644757;
                        if (e.After.Channel.Id == channelCreatorId) // Crear nuevo canal temporal
                        {
                            _ = Task.Run(async () =>
                            {
                                var member = (DiscordMember)e.User;

                                DiscordOverwrite everyoneOverwrite = e.After.Channel.PermissionOverwrites.FirstOrDefault(p => p.Id == 123);
                                var channel = await e.Guild.CreateChannelAsync(name: $"Canal de {member.DisplayName}", type: ChannelType.Voice, parent: e.After.Channel.Parent);
                                await channel.AddOverwriteAsync(member, allow: Permissions.ManageChannels | Permissions.PrioritySpeaker | Permissions.ManageRoles);
                                singleton.AgregarCanalTemporal(channel.Id);
                                await member.ModifyAsync(x => x.VoiceChannel = channel);
                            });
                        }
                    }
                }
            }
            
            return Task.CompletedTask;
        }

        private static Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (!e.Id.StartsWith("modal-"))
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                }
            });
            return Task.CompletedTask;
        }

        private static Task Client_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
        {
            e.Handled = true;
            _ = Task.Run(async () =>
            {
                UsuariosAnilist helper = new();
                var usuario = await helper.GetPerfil(e.Member.Id);
                if (usuario != null)
                {
                    await Funciones.BorrarMensajeUsuarioAnilist(sender, e.Guild, usuario.MessageId);
                    await helper.DeleteAnilist(e.Member.Id);
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

        private static Task TextCommands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (e.Exception is ChecksFailedException ex)
                {
                    foreach (CheckBaseAttribute check in ex.FailedChecks)
                    {
                        string titulo, descripcion;
                        switch (check)
                        {
                            case CooldownAttribute:
                                titulo = "Cooldown";
                                descripcion = "Debes esperar para volver a ejecutar este comando.";
                                break;
                            case RequirePermissionsAttribute:
                                titulo = "Acceso denegado";
                                descripcion = "No tienes los suficientes permisos para ejecutar este comando.";
                                break;
                            case RequireOwnerAttribute:
                                titulo = "Acceso denegado";
                                descripcion = "Solo el dueño del bot puede ejecutar este comando.";
                                break;
                            case RequireNsfwAttribute:
                                titulo = "Requiere NSFW";
                                descripcion = "Este comando debe ser invocado en un canal NSFW.";
                                break;
                            default:
                                titulo = "Error inesperado";
                                descripcion = "Ha ocurrido un error que no puedo manejar.";
                                break;
                        }
                        var miembro = e.Context.Member;
                        DiscordMessage msg = await e.Context.RespondAsync("", embed: new DiscordEmbedBuilder
                        {
                            Title = titulo,
                            Description = descripcion,
                            Color = DiscordColor.Red,
                            Footer = new()
                            {
                                Text = "Invocado por " + miembro.DisplayName + " (" + miembro.Username + "#" + miembro.Discriminator + ")",
                                IconUrl = miembro.AvatarUrl
                            }
                        });
                    }
                }
            });

            return Task.CompletedTask;
        }
    }
}
