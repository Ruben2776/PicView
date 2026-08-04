using PicView.Core.Models;

namespace PicView.Core.Preloading;

/// <summary>
/// Represents a preloaded image value.
/// </summary>
public class PreLoadValue
{
    private readonly Lock _loadingLock = new();
    private bool _isLoading;
    private TaskCompletionSource<bool>? _loadingCompletionSource;
    private int _referenceCount;
    public int ReferenceCount => Volatile.Read(ref _referenceCount);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PreLoadValue"/> class.
    /// </summary>
    /// <param name="imageModel">The image model.</param>
    /// <param name="isLoading">Indicates whether the image is loading.</param>
    public PreLoadValue(ImageModel imageModel, bool isLoading = false)
    {
        ImageModel = imageModel;
        
        if (isLoading)
        {
            // Vital to prevent deadlocks when SetResult is called inside a lock
            _loadingCompletionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }
        IsLoading = isLoading;
    }
    
    /// <summary>
    /// Safely increments the reference count.
    /// </summary>
    public void AddReference() => Interlocked.Increment(ref _referenceCount);

    /// <summary>
    /// Safely decrements the reference count and returns the new count.
    /// </summary>
    public int ReleaseReference() => Interlocked.Decrement(ref _referenceCount);

    /// <summary>
    /// Gets or sets the image model.
    /// </summary>
    public ImageModel ImageModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the image is loading.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            lock (_loadingLock) // Ensure atomic operation
            {
                var wasLoading = _isLoading;
                if (wasLoading == value)
                {
                    return; // No change, exit early
                }

                _isLoading = value;

                // Signal completion when loading changes from true to false
                if (wasLoading && !value && _loadingCompletionSource != null)
                {
                    _loadingCompletionSource.TrySetResult(true);
                    _loadingCompletionSource = null;
                }
                // If we're starting to load, create a new completion source
                else if (value && !wasLoading)
                {
                    _loadingCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                }
            }
        }
    }

    /// <summary>
    /// Gets a task that completes when loading is finished.
    /// </summary>
    /// <returns>A task that completes when IsLoading becomes false.</returns>
    public Task WaitForLoadingCompleteAsync()
    {
        lock (_loadingLock)
        {
            return !_isLoading 
                ? Task.CompletedTask 
                : _loadingCompletionSource?.Task ?? Task.CompletedTask;
        }
    }
}