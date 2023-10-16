using AnilistConEnie.Domain.Firebase;
using Discord;

namespace AnilistConEnie.Infrastructure.Services.Interface
{
    public interface ICacheService
    {
        bool EmoteModeEnabled();
        Emote GetCurrentEmote();
        void ActivarEmoteMode(Emote emote);
        void DesactivarEmoteMode();
        List<UsuarioAnilist> GetusuariosAnilist();
        void SetUsuariosAnilist(List<UsuarioAnilist> usuarios);
        bool EsCanalTemporal(ulong id);
        void AgrgarCanalTemporal(ulong id);
        void EliminarCanalTemporal(ulong id);
        Dictionary<string, Trigger> GetActiveTriggers();
        void SetTrigger(Trigger trigger);
        void RemoveTriggerFromActiveList(string triggerName);
    }
}
