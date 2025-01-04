using System.Collections.Concurrent;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using PicView.Avalonia.ImageHandling;
using PicView.Core.ImageDecoding;
using static System.GC;

namespace PicView.Avalonia.Preloading;

public sealed class PreLoader : IAsyncDisposable
{
    
#if DEBUG

    // ReSharper disable once ConvertToConstant.Local
    private static readonly bool ShowAddRemove = true;
#endif
    
    private readonly PreLoaderConfig _config = new();

    private readonly ConcurrentDictionary<int, PreLoadValue> _preLoadList = new();
    public static bool IsRunning { get; private set; }
    public static int MaxCount => PreLoaderConfig.MaxCount;

    public async Task<bool> AddAsync(int index, List<string> list, ImageModel? imageModel = null)
    {
        if (list == null)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(AddAsync)} list null \n{index}");
#endif
            return false;
        }

        if (index < 0 || index >= list.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(AddAsync)} invalid index: \n{index}");
#endif
            return false;
        }

        var preLoadValue = new PreLoadValue(imageModel);
        try
        {
            var add = _preLoadList.TryAdd(index, preLoadValue);
            if (add)
            {
                if (imageModel is null)
                {
                    preLoadValue.IsLoading = true;
                    var fileInfo = new FileInfo(list[index]);
                    imageModel = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
                    preLoadValue.ImageModel = imageModel;
                }
                else
                {
                    preLoadValue.ImageModel = imageModel;
                    if (imageModel.Image is null)
                    {
                        preLoadValue.IsLoading = true;

                        preLoadValue.ImageModel =
                            await GetImageModel.GetImageModelAsync(imageModel.FileInfo).ConfigureAwait(false);
                    }
                }

                if (imageModel.EXIFOrientation is null)
                {
                    preLoadValue.ImageModel.EXIFOrientation = EXIFHelper.GetImageOrientation(imageModel.FileInfo);
                }

#if DEBUG
                if (ShowAddRemove)
                {
                    Trace.WriteLine($"{imageModel?.FileInfo?.Name} added at {index}");
                }
#endif
                return true;
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(AddAsync)} exception: \n{ex}");
#endif
            return false;
        }
        finally
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (preLoadValue is not null)
            {
                preLoadValue.IsLoading = false;
            }
        }

        return false;
    }

    public async Task<bool> RefreshFileInfo(int index, List<string> list)
    {
        if (list == null)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(RefreshFileInfo)} list null \n{index}");
#endif
            return false;
        }

        if (index < 0 || index >= list.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(RefreshFileInfo)} invalid index: \n{index}");
#endif
            return false;
        }

        var removed = _preLoadList.TryRemove(index, out var preLoadValue);
        if (preLoadValue is null)
        {
            return removed;
        }

        if (preLoadValue.ImageModel != null)
        {
            preLoadValue.ImageModel.FileInfo = null;
        }

        await AddAsync(index, list, preLoadValue.ImageModel).ConfigureAwait(false);
        return removed;
    }

    public void RefreshAllFileInfo(List<string> list)
    {
        try
        {
            foreach (var item in _preLoadList)
            {
                if (item.Value is null)
                {
                    continue;
                }

                var fileInfo = new FileInfo(list[item.Key]);
                if (item.Value.ImageModel != null)
                {
                    item.Value.ImageModel.FileInfo = fileInfo;
                }
            }
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(RefreshAllFileInfo)} \n{e.Message}");
#endif
        }
    }

    /// <summary>
    /// Removes all keys from the cache.
    /// </summary>
    public void Clear()
    {
        foreach (var item in _preLoadList.Values)
        {
            if (item?.ImageModel?.Image is Bitmap img)
            {
                img.Dispose();
            }
        }

        _preLoadList.Clear();
    }

    public PreLoadValue? Get(int key, List<string> list)
    {
        if (list == null)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Get)} list null \n{key}");
#endif
            return null;
        }

        if (key < 0 || key >= list.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Get)} invalid key: \n{key}");
#endif
            return null;
        }

        return !Contains(key, list) ? null : _preLoadList[key];
    }

    public async Task<PreLoadValue?> GetAsync(int key, List<string> list)
    {
        if (list == null)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(GetAsync)} list null \n{key}");
#endif
            return null;
        }

        if (key < 0 || key >= list.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(GetAsync)} invalid key: \n{key}");
#endif
            return null;
        }

        if (Contains(key, list))
        {
            return _preLoadList[key];
        }

        await AddAsync(key, list);
        return Get(key, list);
    }

    public bool Contains(int key, List<string> list)
    {
        if (list == null)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Get)} list null \n{key}");
#endif
            return false;
        }

        if (key < 0 || key >= list.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Contains)} invalid key: \n{key}");
#endif
            return false;
        }

        return _preLoadList.ContainsKey(key);
    }

    public bool Remove(int key, List<string> list)
    {
        if (list == null)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Get)} list null \n{key}");
#endif
            return false;
        }

        if (key < 0 || key >= list.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Remove)} invalid key: \n{key}");
#endif
            return false;
        }

        if (!Contains(key, list))
        {
            return false;
        }

        try
        {
            var item = _preLoadList[key];
            if (item?.ImageModel?.Image is Bitmap img)
            {
                img.Dispose();
            }

            if (item?.ImageModel is not null)
            {
                item.ImageModel.Image = null;
                item.ImageModel.FileInfo = null;
            }

            var remove = _preLoadList.TryRemove(key, out _);
#if DEBUG
            if (remove && ShowAddRemove)
            {
                Trace.WriteLine($"{list[key]} removed at {list.IndexOf(list[key])}");
            }
#endif
            return remove;
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(Remove)} exception:\n{e.Message}");
#endif
            return false;
        }
    }

    public async Task PreLoadAsync(int currentIndex, int count, bool reverse, List<string> list)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            await PreLoadInternalAsync(currentIndex, count, reverse, list, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoadAsync)} exception:\n{exception.Message}");
#endif
        }
    }

    private async Task PreLoadInternalAsync(int currentIndex, int count, bool reverse, List<string> list, CancellationToken token)
    {
        if (list == null)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(PreLoadInternalAsync)} list null \n{currentIndex}");
#endif
            return;
        }

        if (IsRunning)
        {
            return;
        }

        IsRunning = true;

        int nextStartingIndex, prevStartingIndex;
        if (reverse)
        {
            nextStartingIndex = (currentIndex - 1 + count) % count;
            prevStartingIndex = currentIndex + 1;
        }
        else
        {
            nextStartingIndex = (currentIndex + 1) % count;
            prevStartingIndex = currentIndex - 1;
        }

        var array = new int[PreLoaderConfig.MaxCount];

#if DEBUG
        if (ShowAddRemove)
        {
            Trace.WriteLine($"\nPreLoading started at {currentIndex}\n");
        }
#endif

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _config.MaxParallelism,
            CancellationToken = token
        };

        try
        {
            if (reverse)
            {
                await NegativeLoop(options);
                await PositiveLoop(options);
            }
            else
            {
                await PositiveLoop(options);
                await NegativeLoop(options);
            }

            RemoveLoop();
        }
        finally
        {
            IsRunning = false;
        }

        return;

        async Task PositiveLoop(ParallelOptions parallelOptions)
        {
            await Parallel.ForAsync(0, PreLoaderConfig.PositiveIterations, parallelOptions, async (i, _) =>
            {
                if (list.Count == 0 || count != list.Count)
                {
                    Clear();
                    return;
                }

                var index = (nextStartingIndex + i) % list.Count;
                var isAdded = await AddAsync(index, list);
                if (isAdded)
                {
                    array[i] = index;
                }
            });
        }

        async Task NegativeLoop(ParallelOptions parallelOptions)
        {
            await Parallel.ForAsync(0, PreLoaderConfig.NegativeIterations, parallelOptions, async (i, _) =>
            {
                if (list.Count == 0 || count != list.Count)
                {
                    Clear();
                    return;
                }

                var index = (prevStartingIndex - i + list.Count) % list.Count;
                var isAdded = await AddAsync(index, list);
                if (isAdded)
                {
                    array[i] = index;
                }
            });
        }

        void RemoveLoop()
        {
            // Iterate through the _preLoadList and remove items outside the preload range

            if (list.Count <= PreLoaderConfig.MaxCount + PreLoaderConfig.NegativeIterations || _preLoadList.Count <= PreLoaderConfig.MaxCount)
            {
                return;
            }

            var deleteCount = _preLoadList.Count - PreLoaderConfig.MaxCount < PreLoaderConfig.MaxCount
                ? PreLoaderConfig.MaxCount
                : _preLoadList.Count - PreLoaderConfig.MaxCount;
            for (var i = 0; i < deleteCount; i++)
            {
                var removeIndex = reverse ? _preLoadList.Keys.Max() : _preLoadList.Keys.Min();
                if (i >= array.Length)
                {
                    return;
                }

                if (array[i] == removeIndex || removeIndex == currentIndex)
                {
                    continue;
                }

                if (removeIndex > currentIndex + 2 || removeIndex < currentIndex - 2)
                {
                    Remove(removeIndex, list);
                }
            }

            if (deleteCount > 1)
            {
                // Collect unmanaged memory, prevent memory leak
                Collect(0, GCCollectionMode.Optimized, false);
            }
        }
    }

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        SuppressFinalize(this);
        Collect(MaxGeneration, GCCollectionMode.Optimized, false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Clear();
        }

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(false);
    }

    public async ValueTask DisposeAsyncCore()
    {
        await Task.Run(Clear).ConfigureAwait(false);
    }

    ~PreLoader()
    {
        Dispose(false);
    }

    #endregion
}