using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistConEnie.Commands
{
    public class Help : ApplicationCommandModule
    {
        [SlashCommand("help", "Ayuda e informacion del bot")]
        public async Task HelpAsync(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var types = typeof(Program).Assembly.GetTypes();
            var commandTypes = types.Where(type => type.FullName!.StartsWith("AnilistConEnie.Commands", true, CultureInfo.InvariantCulture));

            var sections = GetCategories(commandTypes);

            string description = $"{Formatter.BlockCode("Bot oficial de la comunidad Añilist")}\n";

            sections.ForEach(section =>
            {
                if (section != nameof(Help) && section != nameof(Owner))
                {
                    description += $"{Formatter.Bold(section)}\n";
                    var sectionCommands = GetCategoryCommands(commandTypes, section);
                    var fromGroup = IsSlashCommandGroup(commandTypes, section);

                    sectionCommands.ForEach(cmd =>
                    {
                        if (fromGroup)
                        {
                            description += $"{Formatter.InlineCode($"/{section.ToLower()} {cmd.Name.ToLower()}")} {cmd.Description}\n";
                        }
                        else
                        {
                            description += $"{Formatter.InlineCode($"/{cmd.Name.ToLower()}")} {cmd.Description}\n";
                        }
                    });

                    description += "\n";
                }
            });

            var embed = new DiscordEmbedBuilder
            {
                Title = $"Acerca de {ctx.Client.CurrentUser.Username}",
                Description = Funciones.NormalizarDescription(description),
                Color = Funciones.GetColor(),
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private static List<string> GetCategories(IEnumerable<Type> commandTypes)
        {
            var filteredFromCommands = commandTypes.Where(type => type.ReflectedType == null);
            return filteredFromCommands.Select(type => type.Name).ToList();
        }

        private static bool IsSlashCommandGroup(IEnumerable<Type> commandTypes, string category)
        {
            Type? commandCategory = commandTypes.Where(type => type.ReflectedType == null && type.Name == category).FirstOrDefault();

            if (commandCategory == null)
            {
                return false;
            }

            var att = commandCategory.GetCustomAttributes(typeof(SlashCommandGroupAttribute), false).FirstOrDefault();
            if (att != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static List<SlashCommandAttribute> GetCategoryCommands(IEnumerable<Type> commandTypes, string category)
        {
            var ret = new List<SlashCommandAttribute>();
            Type? commandCategory = commandTypes.Where(type => type.ReflectedType == null && type.Name == category).FirstOrDefault();

            if (commandCategory == null)
            {
                return ret;
            }

            var methods = commandCategory.GetMethods();

            foreach (var method in methods)
            {
                var att = method.GetCustomAttributes(typeof(SlashCommandAttribute), false).FirstOrDefault();
                if (att != null)
                {
                    ret.Add((SlashCommandAttribute)att);
                }
            }

            return ret;
        }
    }
}
