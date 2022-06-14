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

        private static object syncLock = new();

        private ServiciosSingleton()
        {
            _yepMode = false;
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
    }
}
