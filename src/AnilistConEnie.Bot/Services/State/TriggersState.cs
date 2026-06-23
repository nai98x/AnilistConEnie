using AnilistConEnie.Model.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class TriggersState
{
    private readonly ConcurrentDictionary<string, Trigger> _triggers = new();

    public IReadOnlyDictionary<string, Trigger> GetActiveTriggers() => _triggers;

    public void FillTriggers(List<Trigger> triggers)
    {
        _triggers.Clear();
        foreach (Trigger trigger in triggers)
            _triggers[trigger.Nombre] = trigger;
    }

    public void SetTrigger(Trigger trigger) => _triggers[trigger.Nombre] = trigger;

    public void RemoveTriggerFromActiveList(string triggerName) => _triggers.TryRemove(triggerName, out _);
}
