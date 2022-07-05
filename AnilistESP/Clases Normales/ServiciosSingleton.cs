using DSharpPlus.Entities;
using System.Collections.Generic;

namespace AnilistESP
{

    class ServiciosSingleton
    {
        private static ServiciosSingleton _instance;
        private bool _yepMode;
        private DiscordEmoji _emoji;
        private List<UsuarioAnilistFirebase> usuarios;
        private List<ulong> TempVoiceChannels; 

        private static object syncLock = new();

        private ServiciosSingleton()
        {
            _yepMode = false;
            TempVoiceChannels = new();
        }

        public static ServiciosSingleton GetServiciosSingleton()
        {
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new ServiciosSingleton();
                    }
                }
            }

            return _instance;
        }

        public bool YepMode
        {
            get
            {
                return _yepMode;
            }
        }

        public DiscordEmoji Emote
        {
            get
            {
                return _emoji;
            }
        }

        public void ActivarYepmpde(DiscordEmoji emojiNuevo)
        {
            if (!_yepMode)
            {
                _yepMode = true;
                _emoji = emojiNuevo;
            }
            else
            {
                _emoji = emojiNuevo;
            }
        }

        public void DesactivarYepMode()
        {
            if (_yepMode)
            {
                _yepMode = false;
                _emoji = null;
            }
        }

        public List<UsuarioAnilistFirebase> Usuarios
        {
            get
            {
                return usuarios;
            }
        }

        public void SetUsuarios(List<UsuarioAnilistFirebase> newUsers)
        {
            usuarios = newUsers;
        }

        public bool EsCanalTemporal(ulong id)
        {
            return TempVoiceChannels.Contains(id);
        }

        public void AgregarCanalTemporal(ulong id)
        {
            if (!EsCanalTemporal(id)) TempVoiceChannels.Add(id);
        }

        public void EliminarCanalTemporal(ulong id)
        {
            if (!EsCanalTemporal(id)) TempVoiceChannels.Remove(id);
        }
    }
}
