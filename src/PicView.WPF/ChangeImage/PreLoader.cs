using ImageMagick;
using PicView.Core.ImageDecoding;
using PicView.WPF.ImageHandling;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using static PicView.WPF.ChangeImage.Navigation;

namespace PicView.WPF.ChangeImage;

/// <summary>
/// The PreLoader class is responsible for loading and caching images.
/// It contains an internal class named PreLoadValue, which stores information about each image,
/// including the image's BitmapSource and the image's FileInfo object.
/// </summary>
internal static class PreLoader
{
    private static bool _isRunning;

    /// <summary>
    /// Represents a value that is preloaded and cached, including information about the image file.
    /// </summary>
    internal class PreLoadValue
    {
        /// <summary>
        /// The BitmapSource of the image
        /// </summary>
        internal BitmapSource? BitmapSource;

        /// <summary>
        /// FileInfo object of the image file.
        /// </summary>
        internal FileInfo? FileInfo;

        /// <summary>
        /// The orientation of the image
        /// </summary>
        // 0 = none
        // 1 = 0 degrees
        // 2 = 0 degrees, flipped
        // 3 = 180 degrees
        // 4 = 180 degrees, flipped
        // 5 = 270 degrees, flipped
        // 6 = 90 degrees
        // 7 = 90 degrees, flipped
        // 8 = 270 degrees, flipped
        internal ushort? Orientation;

        internal bool IsLoading = true;

        /// <summary>
        /// Constructs a new PreLoadValue object with the specified values
        /// </summary>
        /// <param name="bitmap">The BitmapSource image that is preloaded and cached.</param>
        /// <param name="fileInfo">The file info of the image</param>
        /// <param name="orientation">The orientation of the images</param>
        internal PreLoadValue(BitmapSource? bitmap, FileInfo? fileInfo, ushort? orientation)
        {
            BitmapSource = bitmap;
            FileInfo = fileInfo;
            Orientation = orientation;
        }
    }

    /// <summary>
    /// A dictionary containing the preloaded images
    /// </summary>
    private static readonly ConcurrentDictionary<int, PreLoadValue> PreLoadList = new();

    /// <summary>
    /// Sets the number of iterations to load in front
    /// </summary>
    private const int PositiveIterations = 8;

    /// <summary>
    /// Sets the number of iterations to load behind
    /// </summary>
    private const int NegativeIterations = 4;

    /// <summary>
    /// Set the maximum number of bitmaps to be cached
    /// </summary>
    internal const int MaxCount = PositiveIterations + NegativeIterations + 2;

#if DEBUG

    // ReSharper disable once ConvertToConstant.Local
    private static readonly bool ShowAddRemove = true;

#endif

    /// <summary>
    /// Adds a new image to the PreLoadList dictionary at the specified index.
    /// </summary>
    /// <param name="index">The index at which to add the image.</param>
    /// <param name="fileInfo">The FileInfo object of the image file.</param>
    /// <param name="bitmapSource">The BitmapSource of the image.</param>
    /// <param name="orientation">The orientation of the image.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal static async Task<bool> AddAsync(int index, FileInfo? fileInfo = null, BitmapSource? bitmapSource = null, ushort? orientation = null)
    {
        if (index < 0 || index >= Pics.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(AddAsync)} invalid index: \n{index}");
#endif
            return false;
        }

        try
        {
            var preLoadValue = new PreLoadValue(null, null, 0);
            var add = PreLoadList.TryAdd(index, preLoadValue);
            if (add)
            {
                fileInfo ??= new FileInfo(Pics[index]);
                if (bitmapSource is null)
                {
                    preLoadValue.IsLoading = true;
                    bitmapSource = await Image2BitmapSource.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                }

                preLoadValue.BitmapSource = bitmapSource;
                preLoadValue.FileInfo = fileInfo;
                if (orientation is null)
                {
                    using var magickImage = new MagickImage(fileInfo);
                    preLoadValue.Orientation = EXIFHelper.GetImageOrientation(magickImage);
                }
                else
                {
                    preLoadValue.Orientation = orientation;
                }
                preLoadValue.IsLoading = false;
#if DEBUG
                if (ShowAddRemove)
                    Trace.WriteLine($"{fileInfo.Name} added at {index}");
#endif
                return true;
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(AddAsync)} exception: \n{ex}");
#endif
        }
        return false;
    }

    /// <summary>
    /// Refreshes the file information for the specified index in the PreLoadList dictionary.
    /// </summary>
    /// <param name="index">The index of the image.</param>
    /// <returns>True if the file information was successfully refreshed, false otherwise.</returns>
    internal static async Task<bool> RefreshFileInfo(int index)
    {
        if (index < 0 || index >= Pics.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(RefreshFileInfo)} invalid index: \n{index}");
#endif
            return false;
        }

        var removed = PreLoadList.TryRemove(index, out var preLoadValue);
        if (preLoadValue is not null)
        {
            preLoadValue.FileInfo = null;
        }

        await AddAsync(index, null, preLoadValue?.BitmapSource, preLoadValue?.Orientation).ConfigureAwait(false);
        return removed;
    }

    /// <summary>
    /// Removes all keys from the cache.
    /// </summary>
    internal static void Clear()
    {
        PreLoadList.Clear();
    }

    /// <summary>
    /// Returns the specified BitmapSource.
    /// Returns null if key not found.
    /// </summary>
    /// <param name="key">The corresponding filename</param>
    /// <returns></returns>
    internal static PreLoadValue? Get(int key)
    {
        if (key < 0 || key >= Pics.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Get)} invalid key: \n{key}");
#endif
            return null;
        }

        return !Contains(key) ? null : PreLoadList[key];
    }

    /// <summary>
    /// Checks if the specified key exists
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    internal static bool Contains(int key)
    {
        if (key < 0 || key >= Pics.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Contains)} invalid key: \n{key}");
#endif
            return false;
        }

        return !PreLoadList.IsEmpty && PreLoadList.ContainsKey(key);
    }

    /// <summary>
    /// Removes the key with the specified index, after checking if it exists
    /// </summary>
    /// <param name="key"></param>
    internal static bool Remove(int key)
    {
        if (key < 0 || key >= Pics.Count)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoader)}.{nameof(Remove)} invalid key: \n{key}");
#endif
            return false;
        }

        if (!Contains(key))
        {
            return false;
        }

        try
        {
            _ = PreLoadList[key];
            var remove = PreLoadList.TryRemove(key, out _);
#if DEBUG
            if (remove && ShowAddRemove)
                Trace.WriteLine($"{Pics[key]} removed at {Pics.IndexOf(Pics[key])}");
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

    /// <summary>
    /// An asynchronous task that iterates through file names and caches the decoded images to the PreLoad list
    /// </summary>
    /// <param name="currentIndex">The starting point for the iteration.</param>
    /// <param name="count">The current count of the iterated list</param>
    /// <param name="parallel">Whether to use parallel processing</param>
    internal static async Task PreLoadAsync(int currentIndex, int count, bool parallel)
    {
        if (_isRunning)
        {
            return;
        }
        _isRunning = true;

        int nextStartingIndex, prevStartingIndex;
        var cancellationTokenSource = new CancellationTokenSource();
        if (Reverse)
        {
            nextStartingIndex = (currentIndex - 1 + count) % count;
            prevStartingIndex = currentIndex + 1;
        }
        else
        {
            nextStartingIndex = (currentIndex + 1) % count;
            prevStartingIndex = currentIndex - 1;
        }
        var list = new List<int>();

#if DEBUG
        if (ShowAddRemove)
            Trace.WriteLine($"\nPreLoading started at {currentIndex}\n");
#endif

        var options = parallel
            ? new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount - 2 < 1 ? 1 : Environment.ProcessorCount - 2
            }
            : null;

        try
        {
            if (Reverse)
            {
                await NegativeLoop(options, cancellationTokenSource);
                await PositiveLoop(options, cancellationTokenSource);
            }
            else
            {
                await PositiveLoop(options, cancellationTokenSource);
                await NegativeLoop(options, cancellationTokenSource);
            }
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(PreLoadAsync)} exception:\n{exception.Message}");
#endif
        }
        finally
        {
            _isRunning = false;
        }

        RemoveLoop();

        return;

        async Task PositiveLoop(ParallelOptions parallelOptions, CancellationTokenSource source)
        {
            if (parallel)
            {
                await Parallel.ForAsync(0, PositiveIterations, parallelOptions, async (i, _) =>
                {
                    if (Pics.Count == 0 || count != Pics.Count)
                    {
                        await source.CancelAsync();
                        return;
                    }
                    var index = (nextStartingIndex + i) % Pics.Count;
                    var isAdded = await AddAsync(index);
                    if (isAdded)
                    {
                        list.Add(index);
                    }
                });
            }
            else
            {
                for (var i = 0; i < PositiveIterations; i++)
                {
                    if (Pics.Count == 0 || count != Pics.Count)
                    {
                        await source.CancelAsync();
                        return;
                    }
                    var index = (nextStartingIndex + i) % Pics.Count;
                    var isAdded = await AddAsync(index);
                    if (isAdded)
                    {
                        list.Add(index);
                    }
                }
            }
        }

        async Task NegativeLoop(ParallelOptions parallelOptions, CancellationTokenSource source)
        {
            if (parallel)
            {
                await Parallel.ForAsync(0, NegativeIterations, parallelOptions, async (i, _) =>
                {
                    if (Pics.Count == 0 || count != Pics.Count)
                    {
                        await source.CancelAsync();
                        return;
                    }
                    var index = (prevStartingIndex - i + Pics.Count) % Pics.Count;
                    var isAdded = await AddAsync(index);
                    if (isAdded)
                    {
                        list.Add(index);
                    }
                });
            }
            else
            {
                for (var i = 0; i < NegativeIterations; i++)
                {
                    if (Pics.Count == 0 || count != Pics.Count)
                    {
                        await source.CancelAsync();
                        return;
                    }
                    var index = (prevStartingIndex - i + Pics.Count) % Pics.Count;
                    var isAdded = await AddAsync(index);
                    if (isAdded)
                    {
                        list.Add(index);
                    }
                }
            }
        }

        void RemoveLoop()
        {
            if (Pics.Count <= MaxCount + NegativeIterations || PreLoadList.Count <= MaxCount)
            {
                return;
            }

            for (var i = 0; i < NegativeIterations; i++)
            {
                var removeIndex = Reverse ? PreLoadList.Keys.Max() : PreLoadList.Keys.Min();

                if (!list.Contains(removeIndex))
                {
                    Remove(removeIndex);
                }
            }
        }
    }
}