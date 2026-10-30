using PicView.Core.Navigation;

namespace PicView.Core.Navigation.Interfaces;

/// <summary>
/// Defines the contract for navigating a collection of files within a specific context (Tab).
/// </summary>
public interface IImageIterator : IDisposable
{
    public SharedImageCache Cache { get; }
    public string? CurrentDirectory { get; }

    /// <summary>
    /// The files being iterated. The iterator is the only writer: change the list through <see cref="Initialize"/>,
    /// <see cref="Insert"/>, <see cref="Remove"/>, <see cref="Replace"/> and <see cref="Move"/>, which keep
    /// <see cref="CurrentIndex"/> on the same file and tell the cache and the gallery. Each change publishes a new
    /// snapshot.
    /// </summary>
    IReadOnlyList<FileInfo> Files { get; }

    /// <summary>
    /// Gets the current position in the file list.
    /// </summary>
    int CurrentIndex { get; }
    
    int SecondaryCurrentIndex { get; }

    /// <summary>
    /// Gets a value indicating if the last navigation action was backwards.
    /// </summary>
    bool IsReversed { get; }

    /// <summary>
    /// Manually forces the current index to a specific value.
    /// </summary>
    void SetCurrentIndex(int index);

    /// <summary>
    /// Initializes the iterator with a new list of files and a starting position.
    /// </summary>
    void Initialize(IReadOnlyList<FileInfo> files, int initialIndex = 0);

    /// <summary>
    /// Inserts <paramref name="file"/> at its sorted position, keeping <see cref="CurrentIndex"/> on the same file.
    /// </summary>
    /// <param name="details">The file's details, for the gallery, when the caller has them.</param>
    /// <returns>The position the file was inserted at.</returns>
    int Insert(FileInfo file, Func<string, string, int> compareNames, FileDetailsSource? details = null);

    /// <summary>
    /// Removes the file at <paramref name="path"/>, keeping <see cref="CurrentIndex"/> on the same file. When the
    /// current file itself is removed, <see cref="CurrentIndex"/> stays at its position (clamped to the list), and
    /// navigating from there is the caller's choice.
    /// </summary>
    /// <returns>The position the file was removed from, or -1 if it was not in the list.</returns>
    int Remove(string path);

    /// <summary>
    /// Replaces the entry with the same path as <paramref name="file"/>, e.g., to refresh its size after a change.
    /// </summary>
    /// <param name="details">The file's new details, for the gallery, when the caller has them.</param>
    /// <returns>The position of the file, or -1 if it was not in the list.</returns>
    int Replace(FileInfo file, FileDetailsSource? details = null);

    /// <summary>
    /// Moves the file at <paramref name="oldPath"/> to <paramref name="newFile"/>'s sorted position, e.g., after a
    /// rename. Inserts <paramref name="newFile"/> even if <paramref name="oldPath"/> was not in the list.
    /// <see cref="CurrentIndex"/> follows the current file, under its new name if it was the one moved.
    /// </summary>
    /// <param name="details">The file's details, for the gallery, when the caller has them.</param>
    /// <returns>The position removed from (-1 if none) and the position inserted at.</returns>
    (int Removed, int Inserted) Move(string oldPath, FileInfo newFile, Func<string, string, int> compareNames,
        FileDetailsSource? details = null);

    /// <summary>
    /// Gives the files in the folder <paramref name="oldFolder"/> their paths under <paramref name="newFolder"/>, after
    /// the folder was renamed. The files keep their positions, as the sort orders never compare folders.
    /// </summary>
    /// <returns>The positions of the files that moved.</returns>
    int[] RenameFolder(string oldFolder, string newFolder);

    /// <summary>
    /// Asynchronously navigates the image collection based on the specified navigation direction and skip amount.
    /// </summary>
    /// <param name="to">The direction to navigate, represented by the <see cref="NavigateTo"/> enumeration.</param>
    /// <param name="skipAmount">The amount of images to skip, represented by the <see cref="SkipAmount"/> enumeration.</param>
    /// <param name="ct">The cancellation token source to monitor for task cancellation requests.</param>
    ValueTask NavigateAsync(NavigateTo to, SkipAmount skipAmount, CancellationTokenSource ct);

    /// <summary>
    /// Clears the cache and reloads the current image.
    /// </summary>
    ValueTask ReloadAsync(bool clearCache = true);

    /// <summary>
    /// Moves the iterator to the specified index, triggers image loading, and handles UI updates.
    /// </summary>
    ValueTask IterateToIndexAsync(int index, CancellationTokenSource ct);
    
    /// <summary>
    /// Moves the iterator to the specified indices (for side-by-side viewing), triggers image loading, and handles UI updates.
    /// </summary>
    ValueTask IterateToIndicesAsync(int index, int secondaryIndex, CancellationTokenSource ct);

    /// <summary>
    /// Checks if the target index is cached before iterating. If not cached, clears cache first.
    /// </summary>
    ValueTask SkipToIndexAsync(int index, CancellationTokenSource ct);

    ValueTask NavigateByIncrementsAsync(SkipAmount skipAmount, bool forwards, CancellationTokenSource ct);

    /// <summary>
    /// Handles continuous navigation (e.g., holding down a key) at a set interval.
    /// </summary>
    ValueTask RepeatNavigateAsync(NavigateTo to, TimeSpan repeatInterval, CancellationTokenSource ct);
    
    /// <summary>
    /// Stops the continuous navigation started by <see cref="RepeatNavigateAsync"/>.
    /// </summary>
    void StopRepeatedNavigation();

    /// <summary>
    /// Updates the forward/backward navigation permissions based on the current index and list bounds.
    /// </summary>
    void UpdateNavigationProperties();

    /// <summary>
    /// Notifies the iterator that a new file has been added to the file list.
    /// </summary>
    void NotifyFileAdded();

    /// <summary>
    /// Waits for <paramref name="path"/> to join the file list, as when the folder watcher adds a file just
    /// created in the folder.
    /// </summary>
    /// <returns>Whether the file is in the list, false when it did not join within <paramref name="timeout"/>.</returns>
    ValueTask<bool> WaitForFileAsync(string path, TimeSpan timeout, CancellationToken ct);
}