using Microsoft.Extensions.Configuration;
using TranslatorTRAL;

// ── Configuration ──────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()   // OPENAI__APIKEY overrides appsettings
    .Build();

var apiKey = config["OpenAI:ApiKey"] ?? string.Empty;
var model = config["OpenAI:Model"] ?? "gpt-4o";
var repositoryPath = config["TranslationRepositoryPath"] ?? "data/translations.json";

// Resolve relative path relative to the executable directory
if (!Path.IsPathRooted(repositoryPath))
    repositoryPath = Path.Combine(AppContext.BaseDirectory, repositoryPath);

// ── CLI argument parsing ────────────────────────────────────────────────────────
//
// Usage:
//   TranslatorTRAL translate <input.srt> [<output.srt>]   Translate an SRT file
//   TranslatorTRAL line "<Turkish text>"                  Translate a single line
//   TranslatorTRAL --help                                 Show help

if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
{
    PrintHelp();
    return 0;
}

// ── Validate API key ────────────────────────────────────────────────────────────
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("ERROR: OpenAI API key is not configured.");
    Console.Error.WriteLine("  Set it in appsettings.json  →  OpenAI:ApiKey");
    Console.Error.WriteLine("  or via environment variable →  OPENAI__APIKEY");
    return 2;
}

// ── Load translation repository ─────────────────────────────────────────────────
Console.WriteLine($"Loading translation repository from: {repositoryPath}");
var repository = await TranslationRepository.LoadAsync(repositoryPath);
Console.WriteLine($"Loaded {repository.Entries.Count} translation entries.");

var translator = new TvSeriesTranslator(apiKey, repository, model);

// ── Commands ────────────────────────────────────────────────────────────────────
var command = args[0].ToLowerInvariant();

switch (command)
{
    case "line":
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: TranslatorTRAL line \"<Turkish text>\"");
            return 1;
        }
        var input = string.Join(" ", args.Skip(1));
        Console.WriteLine($"Translating: {input}");
        var result = await translator.TranslateLineAsync(input);
        Console.WriteLine($"Translation: {result}");
        break;
    }

    case "translate":
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: TranslatorTRAL translate <input.srt> [<output.srt>]");
            return 1;
        }

        var inputFile = args[1];
        var outputFile = args.Length >= 3
            ? args[2]
            : Path.Combine(
                Path.GetDirectoryName(inputFile) ?? ".",
                Path.GetFileNameWithoutExtension(inputFile) + "_sq" + Path.GetExtension(inputFile));

        if (!File.Exists(inputFile))
        {
            Console.Error.WriteLine($"Input file not found: {inputFile}");
            return 1;
        }

        Console.WriteLine($"Reading SRT: {inputFile}");
        var blocks = await SubtitleFileHandler.ReadSrtAsync(inputFile);
        Console.WriteLine($"Found {blocks.Count} subtitle blocks.");

        var totalLines = blocks.Sum(b => b.TextLines.Count(l => !string.IsNullOrWhiteSpace(l)));
        Console.WriteLine($"Translating {totalLines} lines using {model}…");

        int done = 0;
        foreach (var block in blocks)
        {
            for (var i = 0; i < block.TextLines.Count; i++)
            {
                var line = block.TextLines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                block.TextLines[i] = await translator.TranslateLineAsync(line);
                done++;
                Console.Write($"\r  Progress: {done}/{totalLines}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Writing output: {outputFile}");
        await SubtitleFileHandler.WriteSrtAsync(outputFile, blocks);
        Console.WriteLine("Done.");
        break;
    }

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
}

return 0;

// ── Helpers ─────────────────────────────────────────────────────────────────────
static void PrintHelp()
{
    Console.WriteLine("""
        TranslatorTRAL — Turkish → Albanian TV Series Subtitle Translator
        Powered by OpenAI · Style-guided by your translation repository

        Usage:
          TranslatorTRAL translate <input.srt> [output.srt]
              Translate an SRT subtitle file from Turkish to Albanian.
              If output path is omitted, a new file with _sq suffix is created.

          TranslatorTRAL line "<Turkish sentence>"
              Translate a single line and print the result.

          TranslatorTRAL --help
              Show this help message.

        Configuration (appsettings.json):
          OpenAI:ApiKey              — Your OpenAI API key (required)
          OpenAI:Model               — Model to use (default: gpt-4o)
          TranslationRepositoryPath  — Path to translations.json (default: data/translations.json)

        Environment variable override:
          OPENAI__APIKEY             — Overrides OpenAI:ApiKey in appsettings.json
        """);
}

