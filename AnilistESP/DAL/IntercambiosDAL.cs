using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class IntercambiosDAL
    {
        private static readonly FirestoreDb db = Funciones.GetFirestoreClient();

        public async Task<List<IntercambiosFirebase>> GetInscriptos(ulong guildId, string tipo)
        {
            List<IntercambiosFirebase> ret = new();

            CollectionReference col = db.Collection("Intercambios").Document($"{guildId}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<IntercambiosFirebase>());
                }
            }

            return ret;
        }

        public async Task<List<IntercambiosFirebase>> GetInscriptosOrdenado(ulong guildId, string tipo)
        {
            List<IntercambiosFirebase> ret = new();

            var query = db.Collection("Intercambios").Document($"{guildId}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios").OrderBy("Orden");
            var snap = await query.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<IntercambiosFirebase>());
                }
            }

            return ret;
        }

        public async Task<List<IntercambiosRecomendacionFirebase>> GetRecomendados(ulong guildId, string tipo)
        {
            List<IntercambiosRecomendacionFirebase> ret = new();

            var query = db.Collection("Intercambios").Document($"{guildId}").Collection("Tipo").Document($"{tipo}").Collection("Recomendaciones");
            var snap = await query.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<IntercambiosRecomendacionFirebase>());
                }
            }

            return ret;
        }

        public async Task<IntercambiosFirebase> GetUserRecomendarA(ulong guildId, ulong userId, string tipo)
        {
            var lista = await GetInscriptosOrdenado(guildId, tipo);
            int index = 0;
            foreach (var item in lista)
            {
                if (item.UserId == (long)userId)
                {
                    IntercambiosFirebase elegido;
                    if (index == lista.Count - 1) // ultimo registro
                    {
                        elegido = lista[0];
                    }
                    else
                    {
                        elegido = lista[index + 1];
                    }

                    return elegido;
                }
                index++;
            }
            return null;
        }

        public async Task ActualizarListaInscriptos(DiscordGuild guild, IntercambiosSettingsFirebase settings, string tipo)
        {
            DiscordChannel canal = guild.GetChannel((ulong)settings.ChannelId);
            DiscordMessage mensaje = await canal.GetMessageAsync((ulong)settings.MessageInscriptosId);
            var embedMsg = mensaje.Embeds[0];
            var lista = await GetInscriptos(guild.Id, tipo);
            string desc = string.Empty;
            foreach (var reg in lista)
            {
                var usuario = await guild.GetMemberAsync((ulong)reg.UserId);
                desc += $"{usuario.Mention}\n" +
                    $"**Preferencias:**\n" +
                    $"  1: {reg.Pref1 ?? "(No asignada)"}\n" +
                    $"  2: {reg.Pref2 ?? "(No asignada)"}\n" +
                    $"**Ban:** {reg.Ban ?? "(No asignado)"}\n\n";
            }
            DiscordEmbedBuilder builder = new(embedMsg);
            builder.Description = desc;
            await mensaje.ModifyAsync(new DiscordMessageBuilder().AddEmbed(builder));
        }

        public async Task ActualizarListaRecomendaciones(InteractionContext ctx, IntercambiosSettingsFirebase settings, string tipo)
        {
            DiscordChannel canal = ctx.Guild.GetChannel((ulong)settings.ChannelId);
            DiscordMessage mensaje = await canal.GetMessageAsync((ulong)settings.MessageRecomendacionesId);
            var embedMsg = mensaje.Embeds[0];
            string desc = string.Empty;

            var inscriptos = await GetInscriptosOrdenado(ctx.Guild.Id, tipo);
            var recomendados = await GetRecomendados(ctx.Guild.Id, tipo);
            List<DiscordUser> noRecomendados = new();

            foreach (var insc in inscriptos)
            {
                if (recomendados.Find(x => x.UserIdRecomendadoPor == insc.UserId) == null)
                {
                    noRecomendados.Add(await ctx.Client.GetUserAsync((ulong)insc.UserId));
                }
            }

            desc += $"\n**Usuarios que les falta recomendar su {tipo.ToLower()}:**\n";
            if (noRecomendados.Count == 0)
            {
                desc += "(Sin registros)\n";
            }
            else
            {
                foreach (var x in noRecomendados)
                {
                    desc += $"- {x.Mention}\n";
                }
            }

            DiscordEmbedBuilder builder = new(embedMsg);
            builder.Description = desc;
            await mensaje.ModifyAsync(new DiscordMessageBuilder().AddEmbed(builder));
        }

        public async Task IniciarInscripcion(InteractionContext ctx, string tipo)
        {
            var embedInscripcion = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Title = "Intercambios - Fase de inscripción",
                Description = "Se ha iniciado la fase de inscripción de los intercambios.\n\n" +
                        "Si quieres participar, utilizanda el comando `/intercambio inscribirse`"
            };

            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();

                if (!registro.Inscripciones && !registro.Elecciones && !registro.Iniciado)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedInscripcion));
                    var msgInscriptos = await ctx.Channel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.CornflowerBlue,
                        Title = "Inscriptos al intercambio"
                    });

                    registro.Inscripciones = true;
                    registro.Elecciones = false;
                    registro.Iniciado = false;
                    registro.ChannelId = (long)ctx.Channel.Id;
                    registro.MessageInscriptosId = (long)msgInscriptos.Id;

                    Dictionary<string, object> data = new()
                    {
                        { "Inscripciones", registro.Inscripciones },
                        { "Elecciones", registro.Elecciones },
                        { "Iniciado", registro.Iniciado },
                        { "ChannelId", registro.ChannelId },
                        { "MessageInscriptosId", registro.MessageInscriptosId },
                        { "MessageRecomendacionesId", registro.MessageRecomendacionesId }
                    };
                    await doc.UpdateAsync(data);
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "Error",
                        Description = "Ya hay un intercambio iniciado"
                    }));
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedInscripcion));
                var msgInscriptos = await ctx.Channel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.CornflowerBlue,
                    Title = "Inscriptos al intercambio"
                });

                Dictionary<string, object> data = new()
                {
                    { "Inscripciones", true },
                    { "Elecciones", false },
                    { "Iniciado", false },
                    { "ChannelId", (long)ctx.Channel.Id },
                    { "MessageInscriptosId", (long)msgInscriptos.Id },
                    { "MessageRecomendacionesId", 1 }
                };
                await doc.CreateAsync(data);
            }
        }

        public async Task IniciarElecciones(InteractionContext ctx, string tipo)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (registro.Inscripciones && !registro.Elecciones && !registro.Iniciado)
                {
                    var lista = await GetInscriptos(ctx.Guild.Id, tipo);
                    lista.Shuffle();
                    var orden = 1;
                    foreach (var r in lista)
                    {
                        DocumentReference docUser = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios").Document($"{r.UserId}");
                        var snapUser = await docUser.GetSnapshotAsync();
                        if (snapUser.Exists)
                        {
                            IntercambiosFirebase reg = snapUser.ConvertTo<IntercambiosFirebase>();
                            reg.Orden = orden;
                            Dictionary<string, object> data = new()
                            {
                                { "UserId", reg.UserId },
                                { "Pref1", reg.Pref1 },
                                { "Pref2", reg.Pref2 },
                                { "Ban", reg.Ban },
                                { "Orden", reg.Orden }
                            };
                            await docUser.UpdateAsync(data);
                        }
                        orden++;
                    }

                    var msgRecomendaciones = await ctx.Channel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Title = "Recomendaciones del intercambio",
                        Color = DiscordColor.CornflowerBlue
                    });

                    Dictionary<string, object> dataSet = new()
                    {
                        { "Inscripciones", false },
                        { "Elecciones", true },
                        { "Iniciado", false },
                        { "ChannelId", registro.ChannelId },
                        { "MessageInscriptosId", registro.MessageInscriptosId },
                        { "MessageRecomendacionesId", msgRecomendaciones.Id }
                    };
                    await doc.UpdateAsync(dataSet);

                    var listInscriptos = await GetInscriptosOrdenado(ctx.Guild.Id, tipo);
                    int index = 0;
                    foreach (var r in listInscriptos)
                    {
                        IntercambiosFirebase elegido;
                        if (index == listInscriptos.Count - 1) // ultimo registro
                        {
                            elegido = listInscriptos[0];
                        }
                        else
                        {
                            elegido = listInscriptos[index + 1];
                        }
                        DiscordMember usuarioDM = await ctx.Guild.GetMemberAsync((ulong)r.UserId);
                        DiscordMember choosen = await ctx.Guild.GetMemberAsync((ulong)elegido.UserId);
                        try
                        {
                            string desc = $"Debes recomendarle un {tipo.ToLower()} a `{choosen.Username}#{choosen.Discriminator}`.\n\n" +
                                $"Entra al canal del intercambio en el servidor e invoca el comando `/intercambio recomendar`\n\n";

                            var servicio = new UsuariosAnilist();
                            var user = await servicio.GetPerfil(ctx.Guild.Id, (ulong)elegido.UserId);
                            if (user != null)
                            {
                                desc += $"**Anilist:** {user.AnilistURL}\n\n";
                            }

                            desc += $"**Preferencias:**\n" +
                                $"  1: {elegido.Pref1 ?? "(No asignada)"}\n" +
                                $"  2: {elegido.Pref2 ?? "(No asignada)"}\n" +
                                $"**Ban:** {elegido.Ban ?? "(No asignado)"}";

                            var channel = await usuarioDM.CreateDmChannelAsync();
                            await channel.SendMessageAsync(new DiscordEmbedBuilder
                            {
                                Color = DiscordColor.Green,
                                Title = $"¡Haz tu recomendación!",
                                Description = desc,
                                Footer = new DiscordEmbedBuilder.EmbedFooter
                                {
                                    IconUrl = ctx.Guild.IconUrl,
                                    Text = ctx.Guild.Name
                                }
                            });
                        }
                        catch (Exception)
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                            {
                                Color = DiscordColor.Yellow,
                                Title = "Intercambios - Mensaje privado no enviado",
                                Description = $"No se ha podido enviar un mensaje privado a {usuarioDM.Mention}"
                            }));
                        }
                        index++;
                    }

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Title = "Intercambios - Fase de elección",
                        Description = $"Debes utilizar el comando `/intercambio reveal` para ver a quien te tocó recomendar.\n\n Una vez tengas un {tipo.ToLower()} para recomendar utiliza el comando `/intercambio recomendar`"
                    }));
                }
                else
                {
                    if (registro.Elecciones)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "El intercambio ya está en fase de elecciones"
                        }));
                    }
                    if (registro.Iniciado)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "El intercambio ya está iniciado"
                        }));
                    }
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "No existe el intercambio"
                }));
            }
        }

        public async Task InscribirseIntercambio(InteractionContext ctx, string tipo, string pref1, string pref2, string ban)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (registro.Inscripciones && !registro.Elecciones && !registro.Iniciado)
                {
                    DocumentReference docUser = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios").Document($"{ctx.User.Id}");
                    var snapUser = await docUser.GetSnapshotAsync();
                    int orden = 0;
                    if (snapUser.Exists)
                    {
                        IntercambiosFirebase reg = snapUser.ConvertTo<IntercambiosFirebase>();

                        reg.UserId = (long)ctx.User.Id;
                        reg.Pref1 = pref1;
                        reg.Pref2 = pref2;
                        reg.Ban = ban;
                        reg.Orden = orden;

                        Dictionary<string, object> data = new()
                        {
                            { "UserId", reg.UserId },
                            { "Pref1", reg.Pref1 },
                            { "Pref2", reg.Pref2 },
                            { "Ban", reg.Ban },
                            { "Orden", reg.Orden }
                        };
                        await docUser.UpdateAsync(data);

                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Title = "Inscripcion",
                            Description = "Tu inscripción ha sido actualizada con éxito"
                        }));
                    }
                    else
                    {
                        Dictionary<string, object> data = new()
                        {
                            { "UserId", (long)ctx.User.Id },
                            { "Pref1", pref1 },
                            { "Pref2", pref2 },
                            { "Ban", ban },
                            { "Orden", orden }
                        };
                        await docUser.CreateAsync(data);

                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Title = "Inscripcion",
                            Description = $"{ctx.Member.Mention} se ha inscripto al intercambio"
                        }));
                    }

                    await ActualizarListaInscriptos(ctx.Guild, registro, tipo);
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "Error",
                        Description = "El intercambio no está en su fase de inscripción"
                    }));
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "El intercambio no se ha creado"
                }));
            }
        }

        public async Task DesinscribirseIntercambio(InteractionContext ctx, string tipo)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (registro.Inscripciones && !registro.Elecciones && !registro.Iniciado)
                {
                    DocumentReference docUser = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios").Document($"{ctx.User.Id}");
                    var snapUser = await docUser.GetSnapshotAsync();
                    if (snapUser.Exists)
                    {
                        await docUser.DeleteAsync();
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Title = "Inscripcion",
                            Description = "Tu inscripción ha sido eliminada con éxito"
                        }));

                        await ctx.Channel.SendMessageAsync(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Title = "Desinscripcion",
                            Description = $"{ctx.Member.Mention} se ha desinscripto del intercambio"
                        });

                        await ActualizarListaInscriptos(ctx.Guild, registro, tipo);
                    }
                    else
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "No estas inscripto al intercambio"
                        }));
                    }
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "Error",
                        Description = "El intercambio no está en su fase de inscripción"
                    }));
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "El intercambio no se ha creado"
                }));
            }
        }

        public async Task RevealIntercambio(InteractionContext ctx, string tipo)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (!registro.Inscripciones && (registro.Elecciones || registro.Iniciado))
                {
                    DocumentReference docUser = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios").Document($"{ctx.User.Id}");
                    var snapUser = await docUser.GetSnapshotAsync();
                    if (snapUser.Exists)
                    {
                        var lista = await GetInscriptosOrdenado(ctx.Guild.Id, tipo);
                        int index = 0;
                        foreach (var item in lista)
                        {
                            if (item.UserId == (long)ctx.User.Id)
                            {
                                IntercambiosFirebase elegido = await GetUserRecomendarA(ctx.Guild.Id, ctx.User.Id, tipo);

                                var miembro = await ctx.Client.GetUserAsync((ulong)elegido.UserId);

                                string desc = $"Tienes que recomendarle un {tipo.ToLower()} a {miembro.Mention}\n\n" +
                                                $"**Preferencias:**\n" +
                                                $"  1: {elegido.Pref1 ?? "(No asignada)"}\n" +
                                                $"  2: {elegido.Pref2 ?? "(No asignada)"}\n" +
                                                $"**Ban:** {elegido.Ban ?? "(No asignado)"}\n";

                                var servicio = new UsuariosAnilist();
                                var user = await servicio.GetPerfil(ctx.Guild.Id, (ulong)elegido.UserId);
                                if (user != null)
                                {
                                    desc += $"\n**Anilist:** {user.AnilistURL}";
                                }

                                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Color = DiscordColor.Green,
                                    Title = "Intercambio",
                                    Description = desc
                                }));

                                return;
                            }
                            index++;
                        }
                    }
                    else
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "No estas inscripto al intercambio"
                        }));
                    }
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "Error",
                        Description = "El intercambio no está en su fase de elección"
                    }));
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "El intercambio no se ha creado"
                }));
            }
        }

        public async Task RecomendarIntercambio(InteractionContext ctx, string tipo, string media)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (!registro.Inscripciones && registro.Elecciones && !registro.Iniciado)
                {
                    DocumentReference docUser = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios").Document($"{ctx.User.Id}");
                    var snapUser = await docUser.GetSnapshotAsync();
                    if (snapUser.Exists)
                    {
                        var result = await FuncionesAnilist.GetAniListMedia(ctx, media, tipo.ToLower());
                        if (result.Ok == true)
                        {
                            IntercambiosFirebase elegido = await GetUserRecomendarA(ctx.Guild.Id, ctx.User.Id, tipo);
                            DocumentReference docNuevo = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Recomendaciones").Document($"{elegido.UserId}");
                            var snapNuevo = await docNuevo.GetSnapshotAsync();

                            if (snapNuevo.Exists)
                            {
                                IntercambiosRecomendacionFirebase registro1 = snapNuevo.ConvertTo<IntercambiosRecomendacionFirebase>();
                                Dictionary<string, object> dataN = new()
                                {
                                    { "UserId", elegido.UserId },
                                    { "UserIdRecomendadoPor", ctx.User.Id },
                                    { "AnimeRecomendadoName", result.TituloRomaji },
                                    { "AnimeRecomendadoURL", result.UrlAnilist },
                                    { "VecesReclamada", registro1.VecesReclamada }
                                };
                                await docNuevo.UpdateAsync(dataN);
                            }
                            else
                            {
                                Dictionary<string, object> dataN = new()
                                {
                                    { "UserId", elegido.UserId },
                                    { "UserIdRecomendadoPor", ctx.User.Id },
                                    { "AnimeRecomendadoName", result.TituloRomaji },
                                    { "AnimeRecomendadoURL", result.UrlAnilist },
                                    { "VecesReclamada", 0 }
                                };
                                await docNuevo.CreateAsync(dataN);
                                await ActualizarListaRecomendaciones(ctx, registro, tipo);
                            }

                            DiscordMember usuarioDM = await ctx.Guild.GetMemberAsync((ulong)elegido.UserId);
                            try
                            {
                                var channel = await usuarioDM.CreateDmChannelAsync();
                                await channel.SendMessageAsync(new DiscordEmbedBuilder
                                {
                                    Color = DiscordColor.Green,
                                    Title = $"¡Te ha llegado tu {tipo.ToLower()} recomendado!",
                                    Description = $"**Nombre:** {result.TituloRomaji}\n**URL:** {result.UrlAnilist}\n\n" +
                                    $"Si consideras esta recomendación troll o por cualquier otro motivo no dudes en hacer una reclamación utilizando el comando `/intercambio reclamar`",
                                    Footer = new DiscordEmbedBuilder.EmbedFooter
                                    {
                                        IconUrl = ctx.Guild.IconUrl,
                                        Text = ctx.Guild.Name
                                    }
                                });
                            }
                            catch (Exception)
                            {
                                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Color = DiscordColor.Yellow,
                                    Title = "Intercambios - Mensaje privado no enviado",
                                    Description = $"No se ha podido enviar un mensaje privado a {usuarioDM.Mention}"
                                }));
                            }

                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
                            {
                                Color = DiscordColor.Green,
                                Title = "Recomendacion",
                                Description = "Haz ingresado tu recomendación correctamente"
                            }));
                        }
                        else
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(result.MsgError));
                        }

                    }
                    else
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "No estas inscripto al intercambio"
                        }));
                    }
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "Error",
                        Description = "El intercambio no está en su fase de elección"
                    }));
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "El intercambio no se ha creado"
                }));
            }
        }

        public async Task IniciarIntercambio(InteractionContext ctx, string tipo)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (!registro.Inscripciones && registro.Elecciones && !registro.Iniciado)
                {
                    var inscriptos = await GetInscriptosOrdenado(ctx.Guild.Id, tipo);
                    var recomendados = await GetRecomendados(ctx.Guild.Id, tipo);
                    List<DiscordUser> noRecomendados = new();

                    foreach (var insc in inscriptos)
                    {
                        if (recomendados.Find(x => x.UserIdRecomendadoPor == insc.UserId) == null)
                        {
                            noRecomendados.Add(await ctx.Client.GetUserAsync((ulong)insc.UserId));
                        }
                    }

                    if (noRecomendados.Count > 0)
                    {
                        string usuarios = string.Empty;
                        foreach (var usr in noRecomendados)
                        {
                            usuarios += $"{usr.Mention}\n";
                        }

                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = $"**Hay usuarios que no han recomendado su {tipo.ToLower()}**\n\n**Estos son:**\n{usuarios}"
                        }));
                    }
                    else
                    {
                        Dictionary<string, object> data = new()
                        {
                            { "Inscripciones", false },
                            { "Elecciones", false },
                            { "Iniciado", true },
                            { "ChannelId", registro.ChannelId },
                            { "MessageInscriptosId", registro.MessageInscriptosId }
                        };
                        await doc.UpdateAsync(data);

                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Title = "Intercambios - Iniciado",
                            Description = "El intercambio se ha iniciado correctamente"
                        }));

                        string descrip = string.Empty;
                        var listt = await GetRecomendados(ctx.Guild.Id, tipo);
                        listt.Shuffle();

                        foreach (var regg in listt)
                        {
                            descrip += $"- [{regg.AnimeRecomendadoName}]({regg.AnimeRecomendadoURL})\n";
                        }

                        await ctx.Channel.SendMessageAsync(new DiscordEmbedBuilder
                        {
                            Title = $"Lista de {tipo.ToLower()}s",
                            Description = descrip,
                            Color = DiscordColor.CornflowerBlue
                        });
                    }
                }
                else
                {
                    if (registro.Inscripciones)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "No se puede hacer esto en la fase de inscripciones"
                        }));
                    }
                    if (registro.Iniciado)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "El intercambio ya está iniciado"
                        }));
                    }
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "No existe el intercambio"
                }));
            }
        }

        public async Task GetRecomendado(InteractionContext ctx, string tipo)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (registro.Elecciones || registro.Iniciado)
                {
                    DocumentReference docNuevo = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Recomendaciones").Document($"{ctx.User.Id}");
                    var snapNuevo = await docNuevo.GetSnapshotAsync();
                    if (snapNuevo.Exists)
                    {
                        IntercambiosRecomendacionFirebase reg = snapNuevo.ConvertTo<IntercambiosRecomendacionFirebase>();
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Title = $"{tipo} recomendado",
                            Description = $"Nombre: {reg.AnimeRecomendadoName}\nURL: {reg.AnimeRecomendadoURL}"
                        }));
                    }
                    else
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = $"Aún no te han recomendado ningún {tipo.ToLower()}"
                        }));
                    }
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "Error",
                        Description = "Debes esperar a la fase de selección para ver tu anime"
                    }));
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "No existe el intercambio"
                }));
            }
        }

        public async Task TerminarIntercambio(InteractionContext ctx, string tipo)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (!registro.Inscripciones && !registro.Elecciones && registro.Iniciado)
                {
                    string descrip = string.Empty;
                    var listt = await GetRecomendados(ctx.Guild.Id, tipo);

                    foreach (var regg in listt)
                    {
                        var user1 = await ctx.Guild.GetMemberAsync((ulong)regg.UserIdRecomendadoPor);
                        var user2 = await ctx.Guild.GetMemberAsync((ulong)regg.UserId);
                        descrip += $"- {user1.Mention} -> [{regg.AnimeRecomendadoName}]({regg.AnimeRecomendadoURL}) -> {user2.Mention}\n";
                    }

                    await ctx.Channel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Title = $"Lista de {tipo.ToLower()}s",
                        Description = descrip,
                        Color = DiscordColor.CornflowerBlue
                    });

                    var inscripcionesRef = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Recomendaciones");
                    IAsyncEnumerable<DocumentReference> subcollections = inscripcionesRef.ListDocumentsAsync();
                    IAsyncEnumerator<DocumentReference> subcollectionsEnumerator = subcollections.GetAsyncEnumerator(default);
                    while (await subcollectionsEnumerator.MoveNextAsync())
                    {
                        DocumentReference subcollectionRef = subcollectionsEnumerator.Current;
                        DocumentReference doc1 = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Recomendaciones").Document(subcollectionRef.Id);
                        var snap1 = await doc1.GetSnapshotAsync();
                        if (snap1.Exists)
                            await doc1.DeleteAsync();
                    }

                    var recomendacionesRef = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios");
                    IAsyncEnumerable<DocumentReference> subcollections1 = recomendacionesRef.ListDocumentsAsync();
                    IAsyncEnumerator<DocumentReference> subcollectionsEnumerator1 = subcollections1.GetAsyncEnumerator(default);
                    while (await subcollectionsEnumerator1.MoveNextAsync())
                    {
                        DocumentReference subcollectionRef = subcollectionsEnumerator1.Current;
                        DocumentReference doc2 = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Usuarios").Document(subcollectionRef.Id);
                        var snap2 = await doc2.GetSnapshotAsync();
                        if (snap2.Exists)
                            await doc2.DeleteAsync();
                    }

                    Dictionary<string, object> data = new()
                    {
                        { "Inscripciones", false },
                        { "Elecciones", false },
                        { "Iniciado", false },
                        { "ChannelId", 1 },
                        { "MessageInscriptosId", 1 }
                    };
                    await doc.UpdateAsync(data);

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Title = "Intercambios - Finalizado",
                        Description = "El intercambio ha finalizado correctamente"
                    }));
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "Error",
                        Description = "Solo puedes terminar el intercambio cuando esté iniciado"
                    }));
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "No existe el intercambio"
                }));
            }
        }

        public async Task HacerReclamacion(InteractionContext ctx, string tipo, string motivo)
        {
            DocumentReference doc = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}");
            var snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IntercambiosSettingsFirebase registro = snap.ConvertTo<IntercambiosSettingsFirebase>();
                if (!registro.Inscripciones && registro.Elecciones && !registro.Iniciado)
                {
                    var lista = await GetRecomendados(ctx.Guild.Id, tipo);
                    var reg = lista.Find(x => x.UserId == (long)ctx.User.Id);
                    if (reg != null)
                    {
                        DocumentReference docNuevo = db.Collection("Intercambios").Document($"{ctx.Guild.Id}").Collection("Tipo").Document($"{tipo}").Collection("Recomendaciones").Document($"{reg.UserId}");
                        var snapNuevo = await docNuevo.GetSnapshotAsync();

                        IntercambiosRecomendacionFirebase registro1 = snapNuevo.ConvertTo<IntercambiosRecomendacionFirebase>();
                        int reclamadaNew = registro1.VecesReclamada + 1;

                        if (reclamadaNew <= 2)
                        {
                            Dictionary<string, object> dataN = new()
                            {
                                { "UserId", registro1.UserId },
                                { "UserIdRecomendadoPor", registro1.UserIdRecomendadoPor },
                                { "AnimeRecomendadoName", registro1.AnimeRecomendadoName },
                                { "AnimeRecomendadoURL", registro1.AnimeRecomendadoURL },
                                { "VecesReclamada", reclamadaNew }
                            };
                            await docNuevo.UpdateAsync(dataN);

                            DiscordMember usuario = await ctx.Guild.GetMemberAsync((ulong)reg.UserId);
                            DiscordMember usuarioRec = await ctx.Guild.GetMemberAsync((ulong)reg.UserIdRecomendadoPor);
                            try
                            {
                                var channel = await usuarioRec.CreateDmChannelAsync();
                                await channel.SendMessageAsync(new DiscordEmbedBuilder
                                {
                                    Color = DiscordColor.Red,
                                    Title = $"Reclamación de {usuario.Username}#{usuario.Discriminator}",
                                    Description = $"Este usuario ha reclamado que no está satisfecho con el {tipo.ToLower()} que le has recomendado.\n\n" +
                                    $"**Motivo:**{Formatter.BlockCode(motivo)}\n" +
                                    $"Le recomendaste [{reg.AnimeRecomendadoName}]({reg.AnimeRecomendadoURL})\n\n" +
                                    $"Si esta reclamación no te convence por algún motivo no dudes en hablarlo con el organizador del intercambio.\n\n" +
                                    $"**Reclamación {reclamadaNew}/2**",
                                    Footer = new DiscordEmbedBuilder.EmbedFooter
                                    {
                                        IconUrl = ctx.Guild.IconUrl,
                                        Text = ctx.Guild.Name
                                    }
                                });
                            }
                            catch (Exception)
                            {
                                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                                {
                                    Color = DiscordColor.Yellow,
                                    Title = "Intercambios - Mensaje privado no enviado",
                                    Description = $"No se ha podido enviar un mensaje privado al usuario que te ha recomendado tu {tipo.ToLower()}"
                                }));
                            }

                            DiscordRole organizadorRole;
                            if (ctx.Guild.Id == 862408834693070898) // Añilist
                            {
                                organizadorRole = ctx.Guild.GetRole(870049184185733120);
                            }
                            else
                            {
                                organizadorRole = ctx.Guild.GetRole(896936028672258098);
                            }
                            var organizadores = ctx.Guild.Members.Where(x => x.Value.IsBot == false && x.Value.Roles.Contains(organizadorRole));
                            foreach (var organizador in organizadores)
                            {
                                try
                                {
                                    var channel = await organizador.Value.CreateDmChannelAsync();
                                    await channel.SendMessageAsync(new DiscordEmbedBuilder
                                    {
                                        Color = DiscordColor.Yellow,
                                        Title = $"Reclamación de {usuario.Username}#{usuario.Discriminator}",
                                        Description = $"Este usuario ha reclamado que no está satisfecho con el {tipo.ToLower()} que le han recomendado.\n" +
                                        $"Motivo:{Formatter.BlockCode(motivo)}\n" +
                                        $"El usuario que ha recomendado es {usuarioRec.Username}#{usuarioRec.Discriminator} y este usuario recomendó [{reg.AnimeRecomendadoName}]({reg.AnimeRecomendadoURL})",
                                        Footer = new DiscordEmbedBuilder.EmbedFooter
                                        {
                                            IconUrl = ctx.Guild.IconUrl,
                                            Text = ctx.Guild.Name
                                        }
                                    });
                                }
                                catch (Exception)
                                {
                                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AsEphemeral(false).AddEmbed(new DiscordEmbedBuilder
                                    {
                                        Color = DiscordColor.Yellow,
                                        Title = "Intercambios - Mensaje privado no enviado",
                                        Description = $"No se ha podido enviar un mensaje privado al organizador {organizador.Value.Mention}"
                                    }));
                                }
                            }

                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                            {
                                Color = DiscordColor.Green,
                                Title = "Reclamación",
                                Description = $"Tu reclamación del intercambio ha sido enviada correctamente.\n\n**Reclamación {reclamadaNew}/2**"
                            }));
                        }
                        else
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                            {
                                Color = DiscordColor.Red,
                                Title = "Error",
                                Description = "Puedes reclamar dos veces como máximo"
                            }));
                        }
                    }
                    else
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "No estas inscripto al intercambio"
                        }));
                    }
                }
                else
                {
                    if (registro.Inscripciones)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "No se puede hacer esto en la fase de inscripciones"
                        }));
                    }
                    if (registro.Iniciado)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "No se puede hacer esto una vez iniciado el intercambio"
                        }));
                    }
                    if (!registro.Inscripciones && !registro.Iniciado)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Title = "Error",
                            Description = "No se puede hacer esto una vez terminado el intercambio"
                        }));
                    }
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Error",
                    Description = "No existe el intercambio"
                }));
            }
        }
    }
}