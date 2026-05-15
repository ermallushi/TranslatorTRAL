using System.Text;
using System.Text.RegularExpressions;

namespace TranslatorTRAL;

/// <summary>
/// Reads and writes SRT subtitle files, extracting only the dialogue lines for translation
/// while preserving the SRT structure (sequence numbers, timecodes, blank lines).
/// </summary>
public static partial class SubtitleFileHandler
{
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex SequenceNumberRegex();

    [GeneratedRegex(@"^\d{2}:\d{2}:\d{2},\d{3} --> \d{2}:\d{2}:\d{2},\d{3}")]
    private static partial Regex TimecodeRegex();

    /// <summary>
    /// Parses an SRT file and returns a list of subtitle blocks.
    /// Each block contains: sequence number line, timecode line, and one or more text lines.
    /// </summary>
    public static async Task<List<SubtitleBlock>> ReadSrtAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
        return ParseSrtLines(lines);
    }

    private static List<SubtitleBlock> ParseSrtLines(string[] lines)
    {
        var blocks = new List<SubtitleBlock>();
        SubtitleBlock? current = null;

        foreach (var line in lines)
        {
            if (SequenceNumberRegex().IsMatch(line.Trim()))
            {
                current = new SubtitleBlock { SequenceLine = line };
                blocks.Add(current);
            }
            else if (current is not null && TimecodeRegex().IsMatch(line.Trim()))
            {
                current.TimecodeLine = line;
            }
            else if (current is not null && current.TimecodeLine is not null)
            {
                current.TextLines.Add(line);
            }
        }

        return blocks;
    }

    /// <summary>
    /// Writes translated subtitle blocks to a new SRT file.
    /// </summary>
    public static async Task WriteSrtAsync(string filePath, IReadOnlyList<SubtitleBlock> blocks)
    {
        var sb = new StringBuilder();

        foreach (var block in blocks)
        {
            sb.AppendLine(block.SequenceLine);
            sb.AppendLine(block.TimecodeLine);
            foreach (var textLine in block.TextLines)
                sb.AppendLine(textLine);
            sb.AppendLine(); // blank line between blocks
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }
}

/// <summary>
/// Represents a single subtitle block in an SRT file.
/// </summary>
public class SubtitleBlock
{
    public string SequenceLine { get; set; } = string.Empty;
    public string? TimecodeLine { get; set; }
    public List<string> TextLines { get; set; } = new();
}
