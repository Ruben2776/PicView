using PicView.Core.Thumbnailing;

namespace PicView.Core.FileSearch;

/// <summary>
/// A file search result as File Search shows it, with a thumbnail for its tooltip.
/// </summary>
/// <param name="result">The matched file and where the match is in its name.</param>
/// <param name="thumbnails">Makes the thumbnail, or null when there is none to make.</param>
/// <param name="owner">The id of the tab the file is in, who keeps the thumbnail.</param>
public sealed class FileSearchItem(FileSearchResult result, Thumbnails? thumbnails, uint owner)
{
    /// <summary>The height of the thumbnail a result shows.</summary>
    private const uint ThumbnailHeight = 150;

    public FileInfo File => result.File;

    /// <inheritdoc cref="FileSearchResult.MatchStartIndex"/>
    public int MatchStartIndex => result.MatchStartIndex;

    /// <inheritdoc cref="FileSearchResult.MatchEndIndex"/>
    public int MatchEndIndex => result.MatchEndIndex;

    /// <summary>
    /// The result's thumbnail, started the first time it is read, e.g., when its tooltip opens.
    /// </summary>
    public Task<object?> GetThumbnail => field ??= LoadThumbnailAsync();

    private async Task<object?> LoadThumbnailAsync()
    {
        if (thumbnails is null || !File.Exists)
        {
            return null;
        }

        return await thumbnails.GetAsync(File, ThumbnailHeight, owner).ConfigureAwait(false);
    }
}
