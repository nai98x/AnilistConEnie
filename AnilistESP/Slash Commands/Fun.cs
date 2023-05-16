using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace AnilistConEnie.Commands
{
    public class Fun : ApplicationCommandModule
    {
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        [SlashRequireBotPermissions(Permissions.ManageWebhooks)]
        [SlashCommand("fakesay", "Usurpa la identidad de un usuario y di algo en su nombre")]
        public async Task FakeSay(InteractionContext ctx, [Option("Usuario", "El usuario del que quieres usurpar su identidad")] DiscordUser usuario, [Option("Mensaje", "El mensaje a replicar")] string mensaje)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.DeleteResponseAsync();

            DiscordMember member = await ctx.Guild.GetMemberAsync(usuario.Id);

            DiscordWebhook? webhook = (await ctx.Channel.GetWebhooksAsync()).FirstOrDefault(wbhk => wbhk.Name.Equals("AnilistConEnie"));
            if (webhook == null)
            {
                webhook = await ctx.Channel.CreateWebhookAsync("AnilistConEnie");
            }

            DiscordWebhookBuilder wBuilder = new DiscordWebhookBuilder()
                .WithContent(mensaje)
                .WithAvatarUrl(member.AvatarUrl)
                .WithUsername(member.DisplayName)
                .AddMentions(Mentions.None.Union(new List<IMention> { new UserMention(), }));
            await webhook.ExecuteAsync(wBuilder);
        }

        [SlashCommand("ship", "Elegir la ship de un usuario")]
        public async Task Ship(InteractionContext ctx, [Option("Usuario", "El usuario del que quieres ver su ship")] DiscordUser usuario = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            usuario ??= ctx.User;
            DiscordMember member = (DiscordMember)usuario;

            Random rnd = new();
            DiscordMember elegido;

            if (ctx.Guild.Id == 862408834693070898)
            {
                DiscordRole tama = ctx.Guild.Roles[1052997018622099548];
                DiscordRole casual = ctx.Guild.Roles[863525487602958336];
                DiscordRole kouhai = ctx.Guild.Roles[865300278491217970];
                DiscordRole senpai = ctx.Guild.Roles[863525246404263976];
                DiscordRole hikikomori = ctx.Guild.Roles[863525128403025961];
                DiscordRole sensei = ctx.Guild.Roles[863524938954571816];
                DiscordRole ousama = ctx.Guild.Roles[966815478507012106];
                DiscordRole teiou = ctx.Guild.Roles[966815813078224907];

                var miembros = ctx.Guild.Members.Where(x => x.Value.IsBot == false && x.Value.Id != usuario.Id &&
                (x.Value.Roles.Contains(tama) || x.Value.Roles.Contains(casual) || x.Value.Roles.Contains(kouhai) || x.Value.Roles.Contains(senpai) || x.Value.Roles.Contains(hikikomori) || x.Value.Roles.Contains(sensei) || x.Value.Roles.Contains(ousama) || x.Value.Roles.Contains(teiou)
                ));
                elegido = miembros.ElementAt(rnd.Next(miembros.Count() - 1)).Value;
            }
            else
            {
                var miembros = ctx.Guild.Members.Where(x => x.Value.IsBot == false && x.Value.Id != usuario.Id);
                elegido = miembros.ElementAt(rnd.Next(miembros.Count() - 1)).Value;
            }

            string shipeoUsr;
            DiscordMember ctxMiembro = await ctx.Guild.GetMemberAsync(usuario.Id);
            shipeoUsr = ctxMiembro.DisplayName;

            string avatar1 = usuario.GetAvatarUrl(ImageFormat.Png, 512);
            string avatar2 = elegido.GetAvatarUrl(ImageFormat.Png, 512);

            byte[] img = await Funciones.MergeImage(avatar1, avatar2, 1024, 512);
            byte[] imagen = Funciones.OverlapImage(img, File.ReadAllBytes(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Images", "frame-love.png")), 1024, 512);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Shippeo",
                Description = $"Shippeo a {ctxMiembro.Mention} con **{elegido.Mention}** 💘",
                ImageUrl = "attachment://imagen.png"
            }).AddFile("imagen.png", Funciones.ToMemoryStream(imagen)));
        }

        [SlashCommand("shiprandom", "Elijo una ship del servidor")]
        public async Task Shiprandom(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Random rnd = new();
            DiscordMember elegido1;
            DiscordMember elegido2;

            IEnumerable<KeyValuePair<ulong, DiscordMember>> miembros;
            if (ctx.Guild.Id == 862408834693070898)
            {
                DiscordRole tama = ctx.Guild.Roles[1052997018622099548];
                DiscordRole casual = ctx.Guild.Roles[863525487602958336];
                DiscordRole kouhai = ctx.Guild.Roles[865300278491217970];
                DiscordRole senpai = ctx.Guild.Roles[863525246404263976];
                DiscordRole hikikomori = ctx.Guild.Roles[863525128403025961];
                DiscordRole sensei = ctx.Guild.Roles[863524938954571816];
                DiscordRole ousama = ctx.Guild.Roles[966815478507012106];
                DiscordRole teiou = ctx.Guild.Roles[966815813078224907];

                miembros = ctx.Guild.Members.Where(x => x.Value.IsBot == false &&
                (x.Value.Roles.Contains(tama) || x.Value.Roles.Contains(casual) || x.Value.Roles.Contains(kouhai) || x.Value.Roles.Contains(senpai) || x.Value.Roles.Contains(hikikomori) || x.Value.Roles.Contains(sensei) || x.Value.Roles.Contains(ousama) || x.Value.Roles.Contains(teiou)
                ));
            }
            else
            {
                miembros = ctx.Guild.Members.Where(x => x.Value.IsBot == false);
            }

            elegido1 = miembros.ElementAt(rnd.Next(miembros.Count() - 1)).Value;
            do
            {
                elegido2 = miembros.ElementAt(rnd.Next(miembros.Count() - 1)).Value;
            } while (elegido1.Id == elegido2.Id);

            string avatar1 = elegido1.GetAvatarUrl(ImageFormat.Png, 512);
            string avatar2 = elegido2.GetAvatarUrl(ImageFormat.Png, 512);

            byte[] img = await Funciones.MergeImage(avatar1, avatar2, 1024, 512);
            byte[] imagen = Funciones.OverlapImage(img, File.ReadAllBytes(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Images", "frame-love.png")), 1024, 512);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Shippeo Random",
                Description = $"Shippeo a {elegido1.Mention} con **{elegido2.Mention}** 💘",
                ImageUrl = "attachment://imagen.png"
            }).AddFile("imagen.png", Funciones.ToMemoryStream(imagen)));
        }

        [SlashCommand("truelove", "Elige el amor veredadero de un usuario")]
        public async Task Truelove(InteractionContext ctx, [Option("Usuario", "El usuario del que quieres ver su ship")] DiscordUser usuario)
        {
            await ctx.DeferAsync();

            int maxPorcentaje = 0;
            DiscordMember match = ctx.Member;
            List<(DiscordMember, int)> amorios = new();

            DiscordRole tama = ctx.Guild.Roles[1052997018622099548];
            DiscordRole casual = ctx.Guild.Roles[863525487602958336];
            DiscordRole kouhai = ctx.Guild.Roles[865300278491217970];
            DiscordRole senpai = ctx.Guild.Roles[863525246404263976];
            DiscordRole hikikomori = ctx.Guild.Roles[863525128403025961];
            DiscordRole sensei = ctx.Guild.Roles[863524938954571816];
            DiscordRole ousama = ctx.Guild.Roles[966815478507012106];
            DiscordRole teiou = ctx.Guild.Roles[966815813078224907];

            var miembros = ctx.Guild.Members;
            foreach(var member in miembros)
            {
                bool tieneRolNecesario = member.Value.Roles.Contains(tama) || member.Value.Roles.Contains(casual) || member.Value.Roles.Contains(kouhai) || member.Value.Roles.Contains(senpai) || member.Value.Roles.Contains(hikikomori) || member.Value.Roles.Contains(sensei) || member.Value.Roles.Contains(ousama) || member.Value.Roles.Contains(teiou);
                if (!member.Value.IsBot && tieneRolNecesario)
                {
                    Random rnd = new((int)(usuario.Id + member.Key));
                    int porcentajeAmor = rnd.Next(0, 100);

                    amorios.Add((member.Value, porcentajeAmor));

                    if (porcentajeAmor > maxPorcentaje)
                    {
                        maxPorcentaje = porcentajeAmor;
                        match = member.Value;
                    }
                }
            }

            var amores = amorios;
            amores.Sort((x, y) => y.Item2.CompareTo(x.Item2));
            amores = amores.Take(5).ToList();
            string amoriosStr = $"**Top 5 pretendientes:**\n{string.Join("\n", amores.Select(x => $"- **{x.Item1.DisplayName}** con un **{x.Item2}%**"))}";

            var odiados = amorios;
            odiados.Sort((x, y) => x.Item2.CompareTo(y.Item2));
            odiados = odiados.Take(5).ToList();
            string odiadosStr = $"**Top 5 odiados:**\n{string.Join("\n", odiados.Select(x => $"- **{x.Item1.DisplayName}** con un **{x.Item2}%**"))}";

            string avatar1 = usuario.GetAvatarUrl(ImageFormat.Png, 512);
            string avatar2 = match.GetAvatarUrl(ImageFormat.Png, 512);

            byte[] img = await Funciones.MergeImage(avatar1, avatar2, 1024, 512);
            byte[] imagen = Funciones.OverlapImage(img, File.ReadAllBytes(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Images", "frame-love.png")), 1024, 512);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "True love",
                Description = $"El amor verdadero de {ctx.Guild.Members[usuario.Id].DisplayName} es **{match.DisplayName}** con un **{maxPorcentaje}%** 💘\n\n{amoriosStr}\n\n{odiadosStr}",
                ImageUrl = "attachment://imagen.png",
                Color = DiscordColor.HotPink
            }).AddFile("imagen.png", Funciones.ToMemoryStream(imagen)));
        }
    }
}
