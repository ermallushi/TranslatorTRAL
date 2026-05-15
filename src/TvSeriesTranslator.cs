using System.ClientModel;
using OpenAI.Chat;

namespace TranslatorTRAL;

/// <summary>
/// Translates Turkish TV series lines to Albanian using OpenAI,
/// strictly following the style established by the translation repository.
/// </summary>
public class TvSeriesTranslator
{
    private readonly ChatClient _chatClient;
    private readonly TranslationRepository _repository;
    private readonly string _model;

    public TvSeriesTranslator(string apiKey, TranslationRepository repository, string model = "gpt-4o")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        _chatClient = new ChatClient(model, new ApiKeyCredential(apiKey));
        _repository = repository;
        _model = model;
    }

    /// <summary>
    /// Translates a single Turkish line to Albanian, following the repository style.
    /// </summary>
    public async Task<string> TranslateLineAsync(string turkishText, CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt();
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(turkishText)
        };

        var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Value.Content[0].Text.Trim();
    }

    /// <summary>
    /// Translates multiple Turkish lines in batch to Albanian, following the repository style.
    /// Lines retain their order and empty lines are preserved as-is.
    /// </summary>
    public async Task<IReadOnlyList<string>> TranslateLinesAsync(
        IEnumerable<string> turkishLines,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var lines = turkishLines.ToList();
        var results = new string[lines.Count];

        var systemPrompt = BuildSystemPrompt();

        for (var i = 0; i < lines.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                results[i] = line;
                progress?.Report(i + 1);
                continue;
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(line)
            };

            var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            results[i] = response.Value.Content[0].Text.Trim();

            progress?.Report(i + 1);
        }

        return results;
    }

    private string BuildSystemPrompt()
    {
        var samples = _repository.GetStyleSamples(30);

        var examplesBlock = string.Join("\n", samples.Select(e =>
            $"TR: {e.Turkish}\nSQ: {e.Albanian}"));

        return $"""
                You are a professional Turkish-to-Albanian subtitle translator for TV series.

                You MUST follow the exact translation style, tone, and vocabulary demonstrated by the examples below.
                These examples come from an existing translation repository and represent the approved style — do NOT deviate from them.

                Rules:
                1. Translate ONLY the Turkish text provided — output ONLY the Albanian translation, nothing else.
                2. Mirror the informal/conversational register used in the examples (TV dialogue, not formal prose).
                3. Keep proper names, place names, and brand names unchanged.
                4. Preserve punctuation style (exclamation marks, question marks, ellipses) as in the examples.
                5. If a line is already in Albanian or is non-Turkish, return it unchanged.
                6. Do NOT add explanations, notes, or any extra text.

                --- STYLE EXAMPLES ---
                {examplesBlock}
                --- END OF EXAMPLES ---

                Now translate the following Turkish line to Albanian following the style above:
                """;
    }
}
