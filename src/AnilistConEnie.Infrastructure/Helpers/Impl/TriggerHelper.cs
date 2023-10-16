using AnilistConEnie.Domain.Enums;
using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Infrastructure.Helpers.Interface;
using AnilistConEnie.Infrastructure.Repositories.Interface;
using AnilistConEnie.Infrastructure.Services.Interface;
using Discord;
using Discord.WebSocket;
using Serilog;

namespace AnilistConEnie.Infrastructure.Helpers.Impl
{
    public class TriggerHelper : ITriggerHelper
    {
        private readonly ICacheService _cacheService;
        private readonly ITriggerRepository _triggerRepository;

        public TriggerHelper(ICacheService cacheService, ITriggerRepository triggerRepository)
        {
            _cacheService = cacheService;
            _triggerRepository = triggerRepository;
        }

        public async Task SetTriggers(bool enabled)
        {
            var triggers = await _triggerRepository.GetTriggers(enabled);
            if (triggers != null)
            {
                foreach(var trigger in triggers)
                {
                    _cacheService.SetTrigger(trigger);
                }
                Log.Information("Triggers cargados");
            }
        }

        public async Task ExecuteTrigger(SocketMessage socketMessage)
        {
            if (!socketMessage.Author.IsBot && !string.IsNullOrEmpty(socketMessage.Content))
            {
                string mensajeOriginal = socketMessage.Content.ToLower();
                var triggers = _cacheService.GetActiveTriggers();
                var matches = triggers.Where(x => mensajeOriginal.Contains(x.Key)).ToList();

                foreach (var trigger in matches)
                {
                    bool validWithType = false;

                    switch ((TipoTrigger)trigger.Value.Tipo)
                    {
                        case TipoTrigger.TEXTO_EXACTO:
                            if (mensajeOriginal == trigger.Key) validWithType = true;
                            break;
                        case TipoTrigger.TERMINA_EN:
                            if (mensajeOriginal.EndsWith(trigger.Key)) validWithType = true;
                            break;
                        case TipoTrigger.EMPIEZA_CON:
                            if (mensajeOriginal.StartsWith(trigger.Key)) validWithType = true;
                            break;
                        case TipoTrigger.LIBRE:
                            validWithType = true;
                            break;
                    }

                    if (validWithType)
                    {
                        var msg = socketMessage as IMessage;
                        var usrMessage = socketMessage as IUserMessage;

                        if (!string.IsNullOrEmpty(trigger.Value.Texto))
                        {
                            if (string.IsNullOrEmpty(trigger.Value.ImageUrl)) /* Solo texto */
                            {
                                await usrMessage.ReplyAsync(trigger.Value.Texto, allowedMentions: AllowedMentions.None);
                            }
                            else /* Texto y imagen */
                            {
                                await usrMessage.ReplyAsync(text: trigger.Value.Texto, embed: new EmbedBuilder().WithImageUrl(trigger.Value.ImageUrl).Build(), allowedMentions: AllowedMentions.None);
                            }
                        }
                        else /* Solo imagen */
                        {
                            await usrMessage.ReplyAsync(embed: new EmbedBuilder().WithImageUrl(trigger.Value.ImageUrl).Build(), allowedMentions: AllowedMentions.None);
                        }
                    }
                }
            }
        }

        public async Task SetTrigger(Trigger trigger)
        {
            await _triggerRepository.SetTrigger(trigger);
        }

        public async Task<Trigger?> EnableTrigger(string triggerName)
        {
            return await _triggerRepository.EnableTrigger(triggerName);
        }

        public async Task<bool> DisableTrigger(string triggerName)
        {
            return await _triggerRepository.DisableTrigger(triggerName);
        }
    }
}
