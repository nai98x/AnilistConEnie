using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class HighlightsState
{
    private readonly ConcurrentDictionary<ulong, List<string>> _highlightedWords = new();

    public void SetHighlightedWords(Dictionary<ulong, List<string>> highlights)
    {
        _highlightedWords.Clear();
        foreach (var (key, value) in highlights)
            _highlightedWords[key] = value;
    }

    public IReadOnlyDictionary<ulong, List<string>> GetHighlightedWords() => _highlightedWords;

    public void AddHighlightedWord(ulong userId, string word)
    {
        _highlightedWords.AddOrUpdate(userId, _ => [word], (_, words) =>
        {
            words.Add(word);
            return words;
        });
    }

    public void RemoveHighlightedWord(ulong userId, string word)
    {
        if (_highlightedWords.TryGetValue(userId, out var words))
            words.Remove(word);
    }
}
