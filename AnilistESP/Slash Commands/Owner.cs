namespace AnilistESP
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using DSharpPlus.SlashCommands.Attributes;
    using Google.Cloud.Firestore;

    [SlashCommandGroup("owner", "Comandos solo disponibles para el owner de Yumiko")]
    [SlashRequireOwner]
    public class Owner : ApplicationCommandModule
    {
        [SlashCommand("test", "Testeos del bot")]
        public async Task Test(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            var usuariosAnilist = await GetListaUsuariosAnilist();
            int i = 0;
        }

        public async Task<List<UsuarioAnilistFirebase>> GetListaUsuariosAnilist()
        {
            var ret = new List<UsuarioAnilistFirebase>();
            FirestoreDb db = Funciones.GetFirestoreClient(Databases.Yumiko);

            CollectionReference col = db.Collection("Anilist").Document($"{862408834693070898}").Collection("Usuarios");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<UsuarioAnilistFirebase>());
                }
            }

            return ret;
        }

        public async Task<List<UsuarioDiscordFirebase>> GetListaCumples()
        {
            var ret = new List<UsuarioDiscordFirebase>();
            FirestoreDb db = Funciones.GetFirestoreClient(Databases.Yumiko);
            CollectionReference col = db.Collection("Cumpleaños").Document($"{862408834693070898}").Collection("Usuarios");
            var snap = await col.GetSnapshotAsync();

            if (snap.Count > 0)
            {
                foreach (var document in snap.Documents)
                {
                    ret.Add(document.ConvertTo<UsuarioDiscordFirebase>());
                }
            }

            return ret;
        }

        [SlashCommand("eliminarguild", "Elimina a Yumiko de un servidor")]
        public async Task EliminarServer(InteractionContext ctx, [Option("Id", "Id del servidor a salirse")] string idStr)
        {
            try
            {
                long id = long.Parse(idStr);
                var guild = await ctx.Client.GetGuildAsync((ulong)id);
                if (guild != null)
                {
                    string nombre = guild.Name;
                    await guild.LeaveAsync();
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"He salido del servidor `{nombre} ({id})`"));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"No se encontró el servidor con la Id `{id}`"));
                }
            }
            catch
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hubo un error obteniendo el servidor con la Id `{idStr}`"));
            }
        }

        [SlashCommand("apagar", "Apaga el bot")]
        public async Task Shutdown(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Apagando...").AsEphemeral(true));
            Environment.Exit(0);
        }
    }
}
