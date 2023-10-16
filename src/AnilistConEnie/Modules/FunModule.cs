using AnilistConEnie.Infrastructure;
using AnilistConEnie.Infrastructure.Handlers;
using AnilistConEnie.Infrastructure.Helpers.Interface;
using Discord;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using System.Data;

namespace AnilistConEnie.Modules
{
    public class FunModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }

        private InteractionHandler _handler;
        private ICommonHelper _commonHelper;
        private readonly Settings _settings;

        public FunModule(InteractionHandler handler, ICommonHelper commonHelper, Settings settings)
        {
            _handler = handler;
            _commonHelper = commonHelper;
            _settings = settings;
        }

        [DefaultMemberPermissions(GuildPermission.ManageGuild)]
        [SlashCommand("fakesay", "Usurpa la identidad de un usuario y di algo en su nombre")]
        public async Task FakeSay([Summary("Usuario", "El usuario del que quieres usurpar su identidad")] IGuildUser member, [Summary("Mensaje", "El mensaje a replicar")] string mensaje)
        {
            await DeferAsync();
            await DeleteOriginalResponseAsync();

            var integrationChannel = Context.Channel as IIntegrationChannel ?? throw new NullReferenceException("IIntegrationChannel null");
            var webhook = (await integrationChannel.GetWebhooksAsync()).FirstOrDefault(wbhk => wbhk.Name.Equals("AnilistConEnie"));
            webhook ??= await integrationChannel.CreateWebhookAsync("AnilistConEnie");

            DiscordWebhookClient client = new DiscordWebhookClient(webhook);
            await client.SendMessageAsync(mensaje, username: member.DisplayName, avatarUrl: member.GetDisplayAvatarUrl(), allowedMentions: AllowedMentions.All);
        }

        [SlashCommand("ship", "Elegir la ship de un usuario")]
        public async Task Ship([Summary("Usuario", "El usuario del que quieres ver su ship")] IGuildUser? user = null)
        {
            await DeferAsync();

            IGuildUser guildUser = Context.User as IGuildUser ?? throw new NullReferenceException("ContextUser (IGuildUser) no deberia ser null");

            IGuildUser usuario;
            if (user is not null) usuario = user;
            else usuario = guildUser;

            Random rnd = new();
            IGuildUser elegido;

            if (Context.Guild.Id == 862408834693070898)
            {
                var roles = Context.Guild.Roles.ToList();

                SocketRole tama = roles.First(x => x.Id == _settings.TamaRole);
                SocketRole casual = roles.First(x => x.Id == _settings.CasualRole);
                SocketRole kouhai = roles.First(x => x.Id == _settings.KouhaiRole);
                SocketRole senpai = roles.First(x => x.Id == _settings.SenpaiRole);
                SocketRole hikikomori = roles.First(x => x.Id == _settings.HikikomoriRole);
                SocketRole sensei = roles.First(x => x.Id == _settings.SenseiRole);
                SocketRole ousama = roles.First(x => x.Id == _settings.OusamaRole);
                SocketRole teiou = roles.First(x => x.Id == _settings.TeiouRole);

                var miembros = Context.Guild.Users.Where(x => x.IsBot == false && x.Id != usuario.Id &&
                (x.Roles.Contains(tama) || x.Roles.Contains(casual) || x.Roles.Contains(kouhai) || x.Roles.Contains(senpai) || x.Roles.Contains(hikikomori) || x.Roles.Contains(sensei) || x.Roles.Contains(ousama) || x.Roles.Contains(teiou)
                ));
                elegido = miembros.ElementAt(rnd.Next(miembros.Count() - 1));
            }
            else
            {
                var miembros = Context.Guild.Users.Where(x => x.IsBot == false && x.Id != usuario.Id);
                elegido = miembros.ElementAt(rnd.Next(miembros.Count() - 1));
            }

            string shipeoUsr = guildUser.DisplayName;

            string avatar1 = usuario.GetAvatarUrl(ImageFormat.Png, 512);
            string avatar2 = elegido.GetAvatarUrl(ImageFormat.Png, 512);

            byte[] img = await _commonHelper.MergeImage(avatar1, avatar2, 1024, 512);
            byte[] imagen = _commonHelper.OverlapImage(img, File.ReadAllBytes(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Images", "frame-love.png")), 1024, 512);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = new EmbedBuilder()
                    .WithTitle("Shippeo")
                    .WithDescription($"Shippeo a {usuario.Mention} con **{elegido.Mention}** 💘")
                    .WithImageUrl("attachment://imagen.png")
                .Build();
                x.Attachments = new[] { new FileAttachment(_commonHelper.ToMemoryStream(imagen), "imagen.png") };
            });
        }

        [SlashCommand("shiprandom", "Elijo una ship del servidor")]
        public async Task Shiprandom()
        {
            await DeferAsync();

            Random rnd = new();
            IGuildUser elegido1;
            IGuildUser elegido2;

            IEnumerable<SocketGuildUser> miembros;
            if (Context.Guild.Id == 862408834693070898)
            {
                var roles = Context.Guild.Roles.ToList();

                SocketRole tama = roles.First(x => x.Id == _settings.TamaRole);
                SocketRole casual = roles.First(x => x.Id == _settings.CasualRole);
                SocketRole kouhai = roles.First(x => x.Id == _settings.KouhaiRole);
                SocketRole senpai = roles.First(x => x.Id == _settings.SenpaiRole);
                SocketRole hikikomori = roles.First(x => x.Id == _settings.HikikomoriRole);
                SocketRole sensei = roles.First(x => x.Id == _settings.SenseiRole);
                SocketRole ousama = roles.First(x => x.Id == _settings.OusamaRole);
                SocketRole teiou = roles.First(x => x.Id == _settings.TeiouRole);

                miembros = Context.Guild.Users.Where(x => x.IsBot == false && x.Id != Context.User.Id &&
                (x.Roles.Contains(tama) || x.Roles.Contains(casual) || x.Roles.Contains(kouhai) || x.Roles.Contains(senpai) || x.Roles.Contains(hikikomori) || x.Roles.Contains(sensei) || x.Roles.Contains(ousama) || x.Roles.Contains(teiou)
                ));
            }
            else
            {
                miembros = Context.Guild.Users.Where(x => x.IsBot == false);
            }

            elegido1 = miembros.ElementAt(rnd.Next(miembros.Count() - 1));
            do
            {
                elegido2 = miembros.ElementAt(rnd.Next(miembros.Count() - 1));
            } while (elegido1.Id == elegido2.Id);

            string avatar1 = elegido1.GetAvatarUrl(ImageFormat.Png, 512);
            string avatar2 = elegido2.GetAvatarUrl(ImageFormat.Png, 512);

            byte[] img = await _commonHelper.MergeImage(avatar1, avatar2, 1024, 512);
            byte[] imagen = _commonHelper.OverlapImage(img, File.ReadAllBytes(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Images", "frame-love.png")), 1024, 512);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = new EmbedBuilder()
                    .WithTitle("Shippeo Random")
                    .WithDescription($"Shippeo a {elegido1.Mention} con **{elegido2.Mention}** 💘")
                    .WithImageUrl("attachment://imagen.png")
                .Build();
                x.Attachments = new[] { new FileAttachment(_commonHelper.ToMemoryStream(imagen), "imagen.png") };
            });
        }

        [SlashCommand("truelove", "Elige el amor veredadero de un usuario")]
        public async Task Truelove([Summary("Usuario", "El usuario del que quieres ver su ship")] IGuildUser usuario)
        {
            await DeferAsync();

            int maxPorcentaje = 0;
            IGuildUser match = Context.User as IGuildUser ?? throw new NullReferenceException("ContextUser (IGuildUser) no deberia ser null");
            List<(IGuildUser, int)> amorios = new();

            var miembros = Context.Guild.Users;
            foreach (var member in miembros)
            {
                bool tieneRolNecesario = true;
                if (Context.Guild.Id == 862408834693070898)
                {
                    var roles = Context.Guild.Roles.ToList();

                    SocketRole senpai = roles.First(x => x.Id == _settings.SenpaiRole);
                    SocketRole hikikomori = roles.First(x => x.Id == _settings.HikikomoriRole);
                    SocketRole sensei = roles.First(x => x.Id == _settings.SenseiRole);
                    SocketRole ousama = roles.First(x => x.Id == _settings.OusamaRole);
                    SocketRole teiou = roles.First(x => x.Id == _settings.TeiouRole);

                    tieneRolNecesario = member.Roles.Contains(senpai) || member.Roles.Contains(hikikomori) || member.Roles.Contains(sensei) || member.Roles.Contains(ousama) || member.Roles.Contains(teiou);
                }

                if (!member.IsBot && tieneRolNecesario && member.Id != usuario.Id)
                {
                    Random rnd = new((int)(usuario.Id + member.Id));
                    int porcentajeAmor = rnd.Next(0, 101);

                    amorios.Add((member, porcentajeAmor));

                    if (porcentajeAmor > maxPorcentaje)
                    {
                        maxPorcentaje = porcentajeAmor;
                        match = member;
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

            byte[] img = await _commonHelper.MergeImage(avatar1, avatar2, 1024, 512);
            byte[] imagen = _commonHelper.OverlapImage(img, File.ReadAllBytes(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Images", "frame-love.png")), 1024, 512);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = new EmbedBuilder()
                    .WithTitle("True love")
                    .WithDescription($"El amor verdadero de {usuario.DisplayName} es **{match.DisplayName}** con un **{maxPorcentaje}%** 💘\n\n{amoriosStr}\n\n{odiadosStr}")
                    .WithImageUrl("attachment://imagen.png")
                .Build();
                x.Attachments = new[] { new FileAttachment(_commonHelper.ToMemoryStream(imagen), "imagen.png") };
            });
        }
    }
}
