namespace PicView.Core.FileSearch;

/// <summary>
/// Represents a file search result, including the file and the location of the matched text.
/// </summary>
/// <param name="File">The matched file.</param>
/// <param name="MatchStartIndex">The starting index of the match within the file's name. Is -1 if the match is not in the name.</param>
/// <param name="MatchEndIndex">The exclusive end index of the match within the file's name. Is -1 if the match is not in the name.</param>
public readonly record struct FileSearchResult(FileInfo File, int MatchStartIndex, int MatchEndIndex);