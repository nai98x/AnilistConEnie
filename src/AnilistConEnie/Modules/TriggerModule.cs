using AnilistConEnie.Domain.Enums;
using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Infrastructure.Handlers;
using AnilistConEnie.Infrastructure.Helpers.Interface;
using AnilistConEnie.Infrastructure.Services.Interface;
using Discord;
using Discord.Interactions;

namespace AnilistConEnie.Modules
{
    [Group("trigger", "Comandos para triggers")]
    public class TriggerModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }

        private InteractionHandler _handler;
        private readonly ITriggerHelper _triggerHelper;
        private readonly ICacheService _cacheService;

        public TriggerModule(InteractionHandler handler, ITriggerHelper triggerHelper, ICacheService cacheService)
        {
            _handler = handler;
            _triggerHelper = triggerHelper;
            _cacheService = cacheService;
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [SlashCommand("set", "Agrega o modifica un trigger")]
        public async Task Set(
            [Summary("Nombre", "Nombre para identificar al trigger")] string nombre,
            [Summary("Tipo", "Tipo de trigger")] TipoTrigger tipo,
            [Summary("Texto", "Texto a mostrar")] string? texto = null,
            [Summary("Imagen", "Url de la imagen a mostrar")] string? imagen = null)
        {
            await DeferAsync();

            if (!string.IsNullOrEmpty(texto) || !string.IsNullOrEmpty(imagen))
            {
                var trigger = new Trigger
                {
                    Nombre = nombre.ToLower(),
                    Texto = texto,
                    ImageUrl = imagen,
                    Activo = true,
                    Tipo = (int)tipo
                };

                await _triggerHelper.SetTrigger(trigger);
                _cacheService.SetTrigger(trigger);

                await ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Trigger agregado")
                        .WithDescription($"Trigger {Format.Code(nombre)} agregado correctamente")
                        .WithColor(Color.Green)
                    .Build();
                });
            }
            else
            {
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription($"El trigger {Format.Code(nombre)} debe tener un texto o una imagen (o ambos)")
                        .WithColor(Color.Red)
                    .Build();
                });
            }
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [SlashCommand("desactivar", "Desactiva un trigger")]
        public async Task Desactivar([Summary("Nombre", "Trigger a desactivar")] string nombre)
        {
            await DeferAsync();

            bool exito = await _triggerHelper.DisableTrigger(nombre);

            if (exito)
            {
                _cacheService.RemoveTriggerFromActiveList(nombre);

                await ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Trigger desactivado")
                        .WithDescription($"Trigger {Format.Code(nombre)} fue desactivado correctamente")
                        .WithColor(Color.Green)
                    .Build();
                });
            }
            else
            {
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription($"Trigger {Format.Code(nombre)} no pudo desactivarse")
                        .WithColor(Color.Red)
                    .Build();
                });
            }
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [SlashCommand("activar", "Activa un trigger")]
        public async Task Activar([Summary("Nombre", "Trigger a activar")] string nombre)
        {
            await DeferAsync();

            Trigger? trigger = await _triggerHelper.EnableTrigger(nombre);

            if (trigger != null)
            {
                _cacheService.SetTrigger(trigger);

                await ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Trigger activado")
                        .WithDescription($"Trigger {Format.Code(nombre)} fue activado correctamente")
                        .WithColor(Color.Green)
                    .Build();
                });
            }
            else
            {
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription($"Trigger {Format.Code(nombre)} no pudo activarse")
                        .WithColor(Color.Red)
                    .Build();
                });
            }
        }

        [SlashCommand("lista", "Ve los triggers")]
        public async Task Lista()
        {
            await DeferAsync();

            var activeTriggers = _cacheService.GetActiveTriggers();
            if (activeTriggers.Any())
            {
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Triggers del server")
                        .WithDescription($"- {string.Join(", ", activeTriggers.Select(x => $"`{x.Key}`"))}")
                        .WithColor(Color.Green)
                    .Build();
                });
            }
            else
            {
                await ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = new EmbedBuilder()
                        .WithTitle("Sin registros")
                        .WithDescription("No hay ningun trigger registrado!")
                        .WithColor(Color.Red)
                    .Build();
                });
            }
        }
    }
}
