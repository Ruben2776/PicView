namespace PicView.Core.Navigation.Interfaces;

/// <summary>
/// Defines the contract for navigating a collection of files within a specific context (Tab).
/// </summary>
public interface IImageIterator : IDisposable
{
    public IImageCache Cache { get; }
    public string? CurrentDirectory { get; }

    /// <summary>
    /// The list of files currently being iterated.
    /// </summary>
    IReadOnlyList<FileInfo> Files { get; internal set; }

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
}