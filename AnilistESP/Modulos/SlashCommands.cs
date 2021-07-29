using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class SlashCommands : SlashCommandModule
    {
        private readonly FuncionesAuxiliares funciones = new();
        private readonly UsuariosAnilist usuariosAnilist = new UsuariosAnilist();

        [SlashCommand("setanilist", "Registra tu AniList en el servidor")]
        public async Task SetAnilist(InteractionContext ctx, [Option("Perfil", "URL o nickname de tu perfil de AniList")] string perfil)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.DeleteResponseAsync();
            var context = funciones.GetContext(ctx);
            await funciones.SetPerfilAnilist(context, perfil);
        }

        [SlashCommand("anilist", "Obtiene un Anilist registrado en el servidor")]
        public async Task Anilist(InteractionContext ctx, [Option("Miembro", "Miembro del que quieres ver el perfil")] DiscordUser user = null, [Option("Secreto", "Si quieres ver solo tu el comando")] bool secreto = false)
        {
            user ??= ctx.User;
            DiscordMember miembro = (DiscordMember)user;
            var userAnilist = await usuariosAnilist.GetPerfil(ctx.Guild.Id, miembro.Id);
            if (userAnilist != null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    IsEphemeral = secreto,
                    Content = $"**Perfil de {miembro.DisplayName} ({miembro.Username}#{miembro.Discriminator})**\n\n{userAnilist.AnilistURL}"
                });
            }
            else
            {
                var builder = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Footer = funciones.GetFooter(ctx),
                    Title = "Perfil no encontrado",
                    Description = $"{miembro.Mention} no tiene ningún usuario de Anilist vinculado con su cuenta.\n\n" +
                                $"Para agregar su perfil, el usuario debe invocar el comando `/setanilist`"
                };
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    IsEphemeral = true
                }.AddEmbed(builder));
            }
        }
    }
}
