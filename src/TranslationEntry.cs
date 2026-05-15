using System.Text.Json.Serialization;

namespace TranslatorTRAL;

/// <summary>
/// Represents a single translation pair in the style repository.
/// </summary>
public class TranslationEntry
{
    [JsonPropertyName("tr")]
    public string Turkish { get; set; } = string.Empty;

    [JsonPropertyName("sq")]
    public string Albanian { get; set; } = string.Empty;
}
