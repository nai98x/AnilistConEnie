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
        private Dictionary<ulong, List<string>> highlightedWords;
        private Dictionary<string, TriggerFirebase> _triggers = new();
        private List<ulong> _spamAccounts = new();

        private static object syncLock = new();

        private ServiciosSingleton()
        {
            _yepMode = false;
            TempVoiceChannels = new();
            highlightedWords = new Dictionary<ulong, List<string>>();
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

        public void SetHighlightedWords(Dictionary<ulong, List<string>> highlights)
        {
            highlightedWords = highlights;
        }

        public Dictionary<ulong, List<string>> GetHighlightedWords()
        {
            return highlightedWords;
        }

        public void AddHighlightedWord(ulong userId, string word)
        {
            bool encontro = highlightedWords.TryGetValue(userId, out var words);
            if (encontro)
            {
                words.Add(word);
                highlightedWords.Add(userId, words);
            }
            else
            {
                List<string> wordList = new()
                {
                    word
                };
                highlightedWords.Add(userId, wordList);
            }
        }

        public void RemoveHighlightedWord(ulong userId, string word)
        {
            bool encontro = highlightedWords.TryGetValue(userId, out var words);
            if (encontro)
            {
                words.Remove(word);
            }
        }

        public Dictionary<string, TriggerFirebase> GetActiveTriggers()
        {
            return _triggers;
        }

        public  void SetTrigger(TriggerFirebase trigger)
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

        public void AddSpamAccount(ulong id)
        {
            _spamAccounts.Add(id);
        }

        public bool IsSpamAccount(ulong id)
        {
            return _spamAccounts.Contains(id);
        }
    }
}
