using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistConEnie.Commands
{
    [SlashCommandGroup("trigger", "Comandos para triggers")]
    public class Triggers : ApplicationCommandModule
    {
        private TriggersDAL triggerService = new();

        [SlashCommand("Set", "Agrega o modifica un trigger")]
        [SlashCommandPermissions(Permissions.ManageMessages)]
        public async Task Set(InteractionContext ctx, [Option("Nombre", "Nombre para identificar al trigger")] string nombre, [Option("Tipo", "Tipo de trigger")] TipoTrigger tipo, [Option("Texto", "Texto a mostrar")] string? texto = null, [Option("Imagen", "Url de la imagen a mostrar")] string? imagen = null)
        {
            await ctx.DeferAsync();

            if (!string.IsNullOrEmpty(texto) || !string.IsNullOrEmpty(imagen))
            {
                ServiciosSingleton singletonServices = ServiciosSingleton.GetServiciosSingleton();

                var trigger = new TriggerFirebase
                {
                    Nombre = nombre,
                    Texto = texto,
                    ImageUrl = imagen,
                    Activo = true,
                    Tipo = (int)tipo
                };

                await triggerService.SetTrigger(trigger);
                singletonServices.SetTrigger(trigger);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Trigger agregado",
                    Description = $"Trigger {Formatter.InlineCode(nombre)} agregado correctamente.",
                    Color = DiscordColor.Green
                }));
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = $"El trigger {Formatter.InlineCode(nombre)} debe tener un texto o una imagen (o ambos)",
                    Color = DiscordColor.Red
                }));
            }
            
        }

        [SlashCommand("desactivar", "Desactiva un trigger")]
        [SlashCommandPermissions(Permissions.ManageMessages)]
        public async Task Desactivar(InteractionContext ctx, [Option("Nombre", "Trigger a desactivar")] string nombre)
        {
            await ctx.DeferAsync();

            ServiciosSingleton singletonServices = ServiciosSingleton.GetServiciosSingleton();

            bool exito = await triggerService.DisableTrigger(nombre);

            if (exito)
            {
                singletonServices.RemoveTriggerFromActiveList(nombre);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Trigger desactivado",
                    Description = $"Trigger {Formatter.InlineCode(nombre)} fue desactivado correctamente.",
                    Color = DiscordColor.Green
                }));
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = $"Trigger {Formatter.InlineCode(nombre)} no pudo desactivarse.",
                    Color = DiscordColor.Red
                }));
            }
        }

        [SlashCommand("activar", "Activa un trigger")]
        [SlashCommandPermissions(Permissions.ManageMessages)]
        public async Task Activar(InteractionContext ctx, [Option("Nombre", "Trigger a activar")] string nombre)
        {
            await ctx.DeferAsync();

            ServiciosSingleton singletonServices = ServiciosSingleton.GetServiciosSingleton();

            TriggerFirebase? trigger = await triggerService.EnableTrigger(nombre);

            if (trigger != null)
            {
                singletonServices.SetTrigger(trigger);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Trigger activado",
                    Description = $"Trigger {Formatter.InlineCode(nombre)} fue activado correctamente.",
                    Color = DiscordColor.Green
                }));
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = $"Trigger {Formatter.InlineCode(nombre)} no pudo activarse.",
                    Color = DiscordColor.Red
                }));
            }
        }

        [SlashCommand("lista", "Ve los triggers")]
        public async Task Listar(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            ServiciosSingleton services = ServiciosSingleton.GetServiciosSingleton();

            var activeTriggers = services.GetActiveTriggers();
            if (activeTriggers.Any())
            {
                string desc = string.Empty;
                var tipos = activeTriggers.GroupBy(x => x.Value.Tipo);

                foreach(var tipo in tipos)
                {
                    var tipoTrigger = (TipoTrigger)tipo.Key;
                    desc += $"**{tipoTrigger.GetName()}**:\n" +
                        $"- {string.Join(", ", activeTriggers.Where(y => y.Value.Tipo == tipo.Key).Select(x => $"`{x.Key}`"))}";
                    desc += "\n\n";
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Triggers del server",
                    Description = Funciones.NormalizarDescription(desc),
                    Color = DiscordColor.Green
                }));
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Sin registros",
                    Description = "No hay ningun trigger registrado!",
                    Color = DiscordColor.Red
                }));
            }
        }
    }
}
