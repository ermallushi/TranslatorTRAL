using System.Text.Json;

namespace TranslatorTRAL;

/// <summary>
/// Loads and provides access to the translation style repository.
/// </summary>
public class TranslationRepository
{
    private readonly List<TranslationEntry> _entries;

    public IReadOnlyList<TranslationEntry> Entries => _entries;

    private TranslationRepository(List<TranslationEntry> entries)
    {
        _entries = entries;
    }

    /// <summary>
    /// Loads translation entries from a JSON file.
    /// </summary>
    public static async Task<TranslationRepository> LoadAsync(string jsonFilePath)
    {
        if (!File.Exists(jsonFilePath))
            throw new FileNotFoundException($"Translation repository not found: {jsonFilePath}");

        await using var stream = File.OpenRead(jsonFilePath);
        var entries = await JsonSerializer.DeserializeAsync<List<TranslationEntry>>(stream)
                      ?? throw new InvalidDataException("Translation repository file is empty or invalid.");

        return new TranslationRepository(entries);
    }

    /// <summary>
    /// Returns a representative sample of translations to use as style examples for the AI prompt.
    /// </summary>
    public IEnumerable<TranslationEntry> GetStyleSamples(int count = 30)
    {
        if (_entries.Count <= count)
            return _entries;

        // Spread samples evenly across the repository for broad style coverage
        var step = _entries.Count / count;
        return Enumerable.Range(0, count).Select(i => _entries[i * step]);
    }
}
