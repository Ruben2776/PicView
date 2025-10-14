using ZLinq;

namespace PicView.Core.FileSearch;

public static class FileSearcher
{
    private const int ExactMatchScore = 100;
    private const int StartsWithScore = 50;
    private const int ContainsInNameScore = 25;
    private const int ContainsInPathScore = 10;
    private const int NoMatchScore = 0;

    public static IEnumerable<FileSearchResult> GetFileSearchResults(List<FileInfo> files, string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return [];
        }

        return files
            // Create an intermediate object with the file and the match result (score and index)
            .Select(f => new { File = f, Match = CalculateMatch(f, userInput) })
            .AsValueEnumerable()
            // Filter out files with no match
            .Where(x => x.Match.Score > NoMatchScore)
            // Order by the highest score
            .OrderByDescending(x => x.Match.Score)
            // Project to the final FileSearchResult type
            .Select(x => new FileSearchResult(
                x.File,
                x.Match.Index,
                x.Match.Index != -1 ? x.Match.Index + userInput.Length : -1
            ))
            .ToArray();
    }

    /// <summary>
    /// Calculates a relevance score and finds the match index within the filename.
    /// </summary>
    /// <returns>A MatchResult containing the Score and the starting Index of the match in the filename.</returns>
    private static MatchResult CalculateMatch(FileInfo fileInfo, string input)
    {
        // Find the first occurrence of the input string in the filename (case-insensitive)
        var indexInName = fileInfo.Name.IndexOf(input, StringComparison.OrdinalIgnoreCase);

        if (indexInName == -1)
        {
            // Match not in name, check the full path as a fallback
            return fileInfo.Name.Contains(input, StringComparison.OrdinalIgnoreCase)
                ? new MatchResult(ContainsInPathScore, -1)
                : new MatchResult(NoMatchScore, -1);
        }

        // Exact match (highest score)
        if (fileInfo.Name.Length == input.Length)
        {
            return new MatchResult(ExactMatchScore, indexInName);
        }

        // Starts with or Contains in name
        return indexInName == 0
            ? new MatchResult(StartsWithScore, indexInName)
            : new MatchResult(ContainsInNameScore, indexInName);
    }

    /// <summary>
    /// Represents the result of a match calculation.
    /// </summary>
    /// <param name="Score">The relevance score of the match.</param>
    /// <param name="Index">The starting index of the match in the filename, or -1 if not in the filename.</param>
    private readonly record struct MatchResult(int Score, int Index);
}