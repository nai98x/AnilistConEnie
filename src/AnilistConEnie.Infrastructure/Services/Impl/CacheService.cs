using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Infrastructure.Services.Interface;
using Discord;

namespace AnilistConEnie.Infrastructure.Services.Impl
{
    public class CacheService : ICacheService
    {
        private bool _emoteMode;
        private Emote? _emote;
        private List<UsuarioAnilist> _usuarios = new();
        private List<ulong> _tempVoiceChannels = new();
        private Dictionary<string, Trigger> _triggers = new();

        public bool EmoteModeEnabled()
        {
            return _emoteMode;
        }

        public Emote GetCurrentEmote()
        {
            if (EmoteModeEnabled()) return _emote ?? throw new NullReferenceException("_emote debe tener un valor si esta habilitado _emoteMode");
            throw new Exception("EmoteModeEnabled() debe ser llamado con anterioridad y ser true");
        }

        public void ActivarEmoteMode(Emote emote)
        {
            if (!_emoteMode) _emoteMode = true;
            _emote = emote;
        }

        public void DesactivarEmoteMode()
        {
            if (_emoteMode)
            {
                _emoteMode = false;
                _emote = null;
            }
        }

        public List<UsuarioAnilist> GetusuariosAnilist()
        {
            return _usuarios;
        }

        public void SetUsuariosAnilist(List<UsuarioAnilist> usuarios)
        {
            _usuarios = usuarios;
        }

        public bool EsCanalTemporal(ulong id)
        {
            return _tempVoiceChannels.Contains(id);
        }

        public void AgrgarCanalTemporal(ulong id)
        {
            if (!EsCanalTemporal(id)) _tempVoiceChannels.Add(id);
        }

        public void EliminarCanalTemporal(ulong id)
        {
            if (!EsCanalTemporal(id)) _tempVoiceChannels.Remove(id);
        }

        public Dictionary<string, Trigger> GetActiveTriggers()
        {
            return _triggers;
        }

        public void SetTrigger(Trigger trigger)
        {
            if (_triggers.TryGetValue(trigger.Nombre, out _))
            {
                _triggers.Remove(trigger.Nombre);
                _triggers.Add(trigger.Nombre, trigger);
            }
            else
            {
                _triggers.Add(trigger.Nombre, trigger);
            }
        }

        public void RemoveTriggerFromActiveList(string triggerName)
        {
            if (_triggers.TryGetValue(triggerName, out _))
            {
                _triggers.Remove(triggerName);
            }
        }
    }
}
