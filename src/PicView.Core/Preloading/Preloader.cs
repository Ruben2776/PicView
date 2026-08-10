using PicView.Core.DebugTools;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;

namespace PicView.Core.Preloading;

/// <summary>
/// Defines the contract for the background worker.
/// <para>
/// Responsible for calculating predictive indices (look-ahead) and executing the physical loading of images into the cache.
/// </para>
/// </summary>
public class Preloader(Func<FileInfo, ValueTask<ImageModel>> imageModelLoader, IImageCache cache)
{
    private readonly Lock _lock = new();
    private uint _currentOwner;
    private bool _isRunning;

    public void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files,
        CancellationToken token)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                if (ownerId == _currentOwner)
                {
                    return; // Already running
                }
                // Currently, it is intended to be able to have multiple different preloaders running at the same time,
                // but if adding features like "Restore Previous Session" this might require a rework.
            }
        
            _isRunning = true; // Mark running immediately so that the next caller (of the same id) is blocked
            _currentOwner = ownerId;
        }

        _ = Task.Run(() => PreLoadInternalAsync(_currentOwner, currentIndex, files, reversed, token), token);
        lock (_lock)
        {
            _currentOwner = ownerId;
        }
    }

    // --- Core Loading Logic (AddAsync) ---

    public async ValueTask<ImageModel?> AddAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list,
        bool isReverse = false, CancellationToken ct = default)
    {
        if (list == null || index < 0 || index >= list.Count)
        {
            return null;
        }

        var fileInfo = list[index];

        // Check if it is already cached
        if (cache.TryGet(fileInfo, out var cachedValue) && cachedValue is not null)
        {
            if (cachedValue.IsLoading)
            {
                // Piggyback on the existing load
                await cachedValue.WaitForLoadingCompleteAsync().ConfigureAwait(false);
            }
            
            // Ensure the requesting owner has a reference to this value in its dictionary
            cache.Add(ownerId, index, cachedValue, list.Count, isReverse);

            return cachedValue.ImageModel;
        }

        if (ct.IsCancellationRequested)
        {
            return null;
        }

        // Load from disk
        var preloadValue = new PreLoadValue(new ImageModel
        {
            FileInfo = fileInfo
        }, isLoading: true);
            
        cache.TryAdd(ownerId, index, preloadValue, list.Count, isReverse, out _);
        // Check cancel before IO
        if (ct.IsCancellationRequested)
        {
            return null;
        }
        var imageModel = await imageModelLoader(fileInfo).ConfigureAwait(false);
        preloadValue.ImageModel = imageModel;
        preloadValue.IsLoading = false;
        return imageModel;
    }

    public async Task PreLoadInternalAsync(uint ownerId, int currentIndex, IReadOnlyList<FileInfo> list,
        bool reversed, CancellationToken token)
    {
        var count = list.Count;
        var nextStartingIndex = (currentIndex + 1) % count;
        var prevStartingIndex = (currentIndex - 1 + count) % count;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = PreLoaderConfig.MaxParallelism,
            CancellationToken = token
        };

        try
        {
            if (reversed)
            {
                await LoopAsync(options, false).ConfigureAwait(false);
                await LoopAsync(options, true).ConfigureAwait(false);
            }
            else
            {
                await LoopAsync(options, true).ConfigureAwait(false);
                await LoopAsync(options, false).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(Preloader), nameof(PreLoadInternalAsync), ex);
        }
        finally
        {
            lock (_lock)
            {
                _isRunning = false;
            }
        }

        return;

        async Task LoopAsync(ParallelOptions parallelOptions, bool positive)
        {
            if (positive)
            {
                await Parallel.ForAsync(0, PreLoaderConfig.PositiveIterations, parallelOptions, async (i, _) =>
                {
                    await AddAddition((nextStartingIndex + i) % count).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            else
            {
                await Parallel.ForAsync(0, PreLoaderConfig.NegativeIterations, parallelOptions, async (i, _) =>
                {
                    await AddAddition((prevStartingIndex - i + count) % count).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        async Task AddAddition(int index)
        {
            token.ThrowIfCancellationRequested();
            // Double check cancellation after waiting
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (cache.Contains(list[index]))
            {
                // Return early if cached
                return;
            }

            await AddAsync(ownerId, index, list, reversed, token).ConfigureAwait(false);
        }
    }
}