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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using NCrontab;
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

        private static CrontabSchedule _schedule;
        private static DateTime _nextRun;

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

            Client.SessionCreated += Client_SessionCreated;
            Client.ClientErrored += Client_ClientError;
            Client.SessionResumed += Client_SessionResumed;
            Client.GuildMemberRemoved += Client_GuildMemberRemoved;
            Client.ComponentInteractionCreated += Client_ComponentInteractionCreated;
            Client.MessageCreated += Client_MessageCreated;
            Client.MessageUpdated += Client_MessageUpdated;
            Client.MessageDeleted += Client_MessageDeleted;
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
                ApplicationCommands.RegisterCommands<Premios>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Intercambios>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<IntercambiosAdmin>(pruebasBacklog);
                ApplicationCommands.RegisterCommands<Triggers>(pruebasBacklog);
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
                ApplicationCommands.RegisterCommands<Premios>(guildProd);
                ApplicationCommands.RegisterCommands<Intercambios>(guildProd);
                ApplicationCommands.RegisterCommands<IntercambiosAdmin>(guildProd);
                ApplicationCommands.RegisterCommands<Triggers>(guildProd);
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

            _schedule = CrontabSchedule.Parse("0 0 * * *");
            _nextRun = _schedule.GetNextOccurrence(DateTime.Now);

            await ScheduledTasks();
        }

        private static async Task ScheduledTasks()
        {
            while (true)
            {
                var now = DateTime.Now;
                _schedule.GetNextOccurrence(now);

                if (now > _nextRun)
                {
                    await Funciones.ManageBirthdayRole(Client);
                    _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                }

                await Task.Delay(5000);
            }
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

                //var highlightService = new HighlightsDAL();
                //var highlights = await highlightService.GetListaHighlights();
                //servicio.SetHighlightedWords(highlights);
                //sender.Logger.LogInformation("Highlights cargadas");

                var triggerService = new TriggersDAL();
                var triggers = await triggerService.GetTriggers(true);
                if (triggers != null)
                {
                    foreach (var trigger in triggers)
                    {
                        servicio.SetTrigger(trigger);
                    }
                }
                sender.Logger.LogInformation("Triggers cargados");

                try
                {
                    var guild = sender.Guilds[862408834693070898];
                    ulong miembro = 862452184029069332;
                    ulong noVinculado = 1117855269943250944;

                    ulong senpai = 863525246404263976;
                    ulong hikikomori = 863525128403025961;
                    ulong sensei = 863524938954571816;
                    ulong ousama = 966815478507012106;
                    ulong teiou = 966815813078224907;

                    DiscordRole coloresExtra = guild.Roles[1034191638714650736];
                    DiscordRole noVinculadoRole = guild.Roles[noVinculado];

                    guild.Members.ToList().ForEach(async member =>
                    {
                        if (!member.Value.Roles.Any(x => x.Id == miembro) && !member.Value.Roles.Any(x => x.Id == noVinculado) && !member.Value.IsBot)
                        {
                            await member.Value.GrantRoleAsync(noVinculadoRole);
                        }

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
                /*var words = service.GetHighlightedWords();
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
                                        string message = $"[{Formatter.Timestamp(e.Message.CreationTimestamp, TimestampFormat.LongTime)}] {e.Author.Username}: {e.Message.Content}";

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
                }*/
                #endregion

                #region Triggers
                if (!e.Author.IsBot && !string.IsNullOrEmpty(e.Message.Content))
                {
                    var mensajeOriginal = e.Message.Content;
                    var triggers = service.GetActiveTriggers();
                    var matches = triggers.Where(x => mensajeOriginal.Contains(x.Value.Texto)).ToList();

                    foreach(var trigger in matches)
                    {
                        bool validWithType = false;

                        switch ((TipoTrigger)trigger.Value.Tipo)
                        {
                            case TipoTrigger.TEXTO_EXACTO:
                                if (mensajeOriginal == trigger.Value.Texto) validWithType = true;
                                break;
                            case TipoTrigger.TERMINA_EN:
                                if (mensajeOriginal.EndsWith(trigger.Value.Texto)) validWithType = true;
                                break;
                            case TipoTrigger.EMPIEZA_CON:
                                if (mensajeOriginal.StartsWith(trigger.Value.Texto)) validWithType = true;
                                break;
                            case TipoTrigger.LIBRE:
                                validWithType = true;
                                break;
                        }

                        if (validWithType)
                        {
                            var messageBuilder = new DiscordMessageBuilder();

                            if (!string.IsNullOrEmpty(trigger.Value.Texto))
                            {
                                messageBuilder.WithContent(trigger.Value.Texto);
                            }

                            if (!string.IsNullOrEmpty(trigger.Value.ImageUrl))
                            {
                                messageBuilder.AddEmbed(
                                    new DiscordEmbedBuilder()
                                        .WithImageUrl(trigger.Value.ImageUrl)
                                        .WithColor(Funciones.GetColor())
                                    .Build()
                                );
                            }

                            await e.Message.RespondAsync(messageBuilder);
                        }
                    }
                }
                #endregion

                #region Intercambios Repost
                if (e.Guild?.Id == 862408834693070898)
                {
                    if (e.Channel.ParentId == 1048075286626979861)
                    {
                        IntercambiosRepostDAL service = new();
                        var forumChannel = e.Channel.Parent as DiscordForumChannel;
                        var forumPost = e.Channel as DiscordThreadChannel;
                        var autor = e.Message.Author as DiscordMember;
                        var images = e.Message.Attachments.Where(x => x.MediaType.StartsWith("image/")).Take(5).ToList();
                        var messageBuilder = new DiscordMessageBuilder().WithContent($"{e.Message.JumpLink}");
                        DiscordChannel repostChannel;
                        var embed = new DiscordEmbedBuilder()
                            .WithDescription(e.Message.Content)
                            .WithAuthor(autor.DisplayName, iconUrl: autor.GuildAvatarUrl ?? autor.AvatarUrl)
                            .WithColor(DiscordColor.Green);

                        if (images.Count > 0)
                        {
                            bool first = true;
                            foreach (var image in images)
                            {
                                if (first)
                                {
                                    embed.ImageUrl = image.Url;
                                    messageBuilder.AddEmbed(embed);
                                    first = false;
                                }
                                else
                                {
                                    messageBuilder.AddEmbed(new DiscordEmbedBuilder().WithImageUrl(image.Url));
                                }
                            }
                        }
                        else
                        {
                            messageBuilder.AddEmbed(embed);
                        }

                        if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "anime"))
                        {
                            repostChannel = e.Guild.Channels[862432891186839572];
                            var msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                            await service.SetMensaje(e.Message.ChannelId, e.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                        }
                        if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "manga"))
                        {
                            repostChannel = e.Guild.Channels[882003534797742130];
                            var msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                            await service.SetMensaje(e.Message.ChannelId, e.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                        }
                        if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "pelis"))
                        {
                            repostChannel = e.Guild.Channels[865319767967793152];
                            var msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                            await service.SetMensaje(e.Message.ChannelId, e.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                        }
                        if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "series"))
                        {
                            repostChannel = e.Guild.Channels[1125578754790543380];
                            var msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                            await service.SetMensaje(e.Message.ChannelId, e.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                        }
                        if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "música"))
                        {
                            repostChannel = e.Guild.Channels[862419584065732618];
                            var msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                            await service.SetMensaje(e.Message.ChannelId, e.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                        }
                    }
                }
                #endregion
            });
            return Task.CompletedTask;
        }

        private static Task Client_MessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                #region Intercambios Repost
                if (e.Guild?.Id == 862408834693070898)
                {
                    if (e.Channel.ParentId == 1048075286626979861)
                    {
                        IntercambiosRepostDAL service = new();
                        MensajeIntercambioRepostFirebase? mensaje = await service.GetMensaje(e.Message.Id);

                        if (mensaje != null)
                        {
                            var forumChannel = e.Channel.Parent as DiscordForumChannel;
                            var forumPost = e.Channel as DiscordThreadChannel;
                            var autor = e.Message.Author as DiscordMember;
                            DiscordChannel? repostChannel = e.Guild.Channels[mensaje.IdCanalMensajeRepost];
                            if (repostChannel != null )
                            {
                                try
                                {
                                    DiscordMessage repostMessage = await repostChannel.GetMessageAsync(mensaje.IdMensajeRepost, true);
                                    DiscordEmbed embed = repostMessage.Embeds.First();

                                    var newEmbed = new DiscordEmbedBuilder(embed);
                                    newEmbed.Description = e.Message.Content;

                                    await repostMessage.ModifyAsync(embed: newEmbed.Build());
                                }
                                catch (Exception) { /* Ignored */}
                            }
                        }
                    }
                }
                #endregion
            });
            return Task.CompletedTask;
        }

        private static Task Client_MessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                #region Intercambios Repost
                if (e.Guild?.Id == 862408834693070898)
                {
                    if (e.Channel.ParentId == 1048075286626979861)
                    {
                        IntercambiosRepostDAL service = new();
                        MensajeIntercambioRepostFirebase? mensaje = await service.GetMensaje(e.Message.Id);

                        if (mensaje != null)
                        {
                            var forumChannel = e.Channel.Parent as DiscordForumChannel;
                            var forumPost = e.Channel as DiscordThreadChannel;
                            var autor = e.Message.Author as DiscordMember;
                            DiscordChannel? repostChannel = e.Guild.Channels[mensaje.IdCanalMensajeRepost];
                            if (repostChannel != null)
                            {
                                try
                                {
                                    DiscordMessage repostMessage = await repostChannel.GetMessageAsync(mensaje.IdMensajeRepost, true);
                                    await repostChannel.DeleteMessageAsync(repostMessage);
                                    await service.DeleteMensaje(e.Message.Id);
                                }
                                catch (Exception) { /* Ignored */}
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
                                var miembroRole = e.Guild.Roles[862452184029069332];
                                var botRole = e.Guild.Roles[862411811226910730];

                                var channel = await e.Guild.CreateChannelAsync(name: $"Canal de {member.DisplayName}", type: ChannelType.Voice, parent: e.After.Channel.Parent);
                                await channel.AddOverwriteAsync(member, allow: Permissions.ManageChannels | Permissions.PrioritySpeaker | Permissions.ManageRoles);
                                await channel.AddOverwriteAsync(e.Guild.EveryoneRole, deny: Permissions.AccessChannels);
                                await channel.AddOverwriteAsync(miembroRole, allow: Permissions.AccessChannels);
                                await channel.AddOverwriteAsync(botRole, allow: Permissions.AccessChannels);

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
                if (e.Id.StartsWith("modal-anilistprofileset"))
                {
                    await FuncionesAnilist.VincularAniList(e.Interaction, sender, e);
                }
                else
                {
                    if (!e.Id.StartsWith("modal-"))
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    }
                }
            });
            return Task.CompletedTask;
        }

        private static Task Client_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                #region Mensaje de despedida
                if (!e.Member.IsBot)
                {
                    ulong miembroRole = 862452184029069332;
                    ulong noVerificadoRole = 1117855269943250944;

                    if (e.Member.Roles.Any(x => x.Id == miembroRole) || e.Member.Roles.Any(x => x.Id == noVerificadoRole))
                    {
                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
                        embedBuilder.WithTitle($"{e.Member.DisplayName} se ha ido del servidor");
                        embedBuilder.WithColor(DiscordColor.Red);

                        if (e.Member.Roles.Any(x => x.Id == miembroRole))
                        {
                            var images = new List<string>()
                            {
                                "https://media.discordapp.net/attachments/867856756901937202/1128343142685495357/cowboy-bebop-bang.gif",
                                "https://images-ext-1.discordapp.net/external/JNe44k_TxRaSDaTrk4yKXF4bNScwz1H2QyVcfq4Q7lI/https/media.tenor.com/8lKrNgbJ7PoAAAPo/adi%25C3%25B3s-vaquero-adios.mp4",
                                "https://images-ext-1.discordapp.net/external/PUyQvhBLtt9qZ2FVc2-f-1PxFZXtmPayOk2t5KelNCY/https/media.tenor.com/p5DlOqiAhMsAAAPo/adios-vaquero.mp4"
                            };

                            var worrysad = DiscordEmoji.FromGuildEmote(sender, 862730038860316672);
                            embedBuilder.WithDescription($"RIP {e.Member.Mention} {worrysad}");
                            embedBuilder.WithImageUrl(images[Funciones.GetNumeroRandom(0, images.Count)]);
                        }
                        else
                        {
                            embedBuilder.WithImageUrl("https://media.discordapp.net/attachments/816379048477065217/1129080799585648670/Sin_titulo-1s.png");
                        }

                        DiscordGuild guild = sender.Guilds[862408834693070898];
                        DiscordChannel channel = guild.Channels[862408834693070901];

                        await channel.SendMessageAsync(embedBuilder.Build());
                    }
                }
                #endregion

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

        private static Task Client_SessionCreated(DiscordClient c, SessionReadyEventArgs e)
        {
            c.Logger.LogInformation("El cliente esta listo para procesar eventos.", DateTime.Now);
            return Task.CompletedTask;
        }

        private static Task Client_SessionResumed(DiscordClient c, SessionReadyEventArgs e)
        {
            c.Logger.LogInformation("El cliente vuelve a estar listo para procesar eventos.", DateTime.Now);
            return Task.CompletedTask;
        }

        private static Task Client_ClientError(DiscordClient c, ClientErrorEventArgs e)
        {
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
            _ = Task.Run(async () =>
            {
                await LogChannel.SendMessageAsync(Funciones.LogInteractionCommand(e, "Slash Command ejecutado", true, false));
            });
            return Task.CompletedTask;
        }

        private static Task SlashCommands_SlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
        {
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
                                Text = "Invocado por " + miembro.DisplayName + " (" + miembro.Username + ")",
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
