using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Services.State;

public class EmoteModeState
{
    public bool YepMode { get; private set; }

    public DiscordEmoji? Emote { get; private set; }

    public void ActivarYepMode(DiscordEmoji emojiNuevo)
    {
        YepMode = true;
        Emote = emojiNuevo;
    }

    public void DesactivarYepMode()
    {
        if (!YepMode) return;
        YepMode = false;
        Emote = null;
    }
}
