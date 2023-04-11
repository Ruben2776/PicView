using PicView.FileHandling;
using PicView.ImageHandling;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;

namespace PicView.ChangeImage
{

    /// <summary>
    /// The Preloader class is responsible for loading and caching images.
    /// It contains an internal class named PreloadValue, which stores information about each image,
    /// including the image's BitmapSource, whether the image is currently being loaded, and the image's FileInfo object.
    /// </summary>
    internal static class Preloader
    {
        /// <summary>
        /// Represents a value that is preloaded and cached, including information about the image file and whether it is currently being loaded.
        /// </summary>
        internal class PreloadValue
        {
            /// <summary>
            /// The BitmapSource of the image
            /// </summary>
            internal BitmapSource? BitmapSource;

            /// <summary>
            /// Whether the image is currently being loaded
            /// </summary>
            internal bool IsLoading;

            /// <summary>
            /// FileInfo object of the image file.
            /// </summary>
            internal FileInfo? FileInfo;

            /// <summary>
            /// Constructs a new PreloadValue object with the specified values
            /// </summary>
            /// <param name="bitmap">The BitmapSource image that is preloaded and cached.</param>
            /// <param name="loading">Whether the image is currently being loaded</param>
            /// <param name="fileInfo">The file info of the image</param>
            internal PreloadValue(BitmapSource? bitmap, bool loading, FileInfo? fileInfo)
            {
                BitmapSource = bitmap;
                IsLoading = loading;
                FileInfo = fileInfo;
            }
        }

        /// <summary>
        /// A dictionary containing the preloaded images
        /// </summary>
        private static readonly ConcurrentDictionary<int, PreloadValue> PreloadList = new();

        /// <summary>
        /// Sets the number of iterations to load infront 
        /// </summary>
        private const int PositiveIterations = 8;

        /// <summary>
        /// Sets the number of iterations to load behind
        /// </summary>
        private const int NegativeIterations = 4;

        /// <summary>
        /// Set the maximum number of bitmaps to be cached
        /// </summary>
        internal const int MaxCount = PositiveIterations + NegativeIterations + 1;

#if DEBUG
        // ReSharper disable once ConvertToConstant.Local
        private static readonly bool ShowAddRemove = false;
#endif

        /// <summary>
        /// Adds a file to the preloader from the specified index. Returns true if a new value was added.
        /// </summary>
        /// <param name="index">Index of the image in the list of Pics</param>
        /// <param name="fileInfo">The file info of the image</param>
        /// <param name="bitmapSource">The bitmap source of the image</param>
        /// <returns>Preloadvalue that can be null</returns>
        internal static async Task AddAsync(int index, FileInfo? fileInfo = null, BitmapSource? bitmapSource = null)
        {
            if (index < 0 || index >= Pics.Count) return;

            try
            {
                var preloadValue = new PreloadValue(null, true, null);
                var add = PreloadList.TryAdd(index, preloadValue);
                if (add)
                {
                    fileInfo ??= new FileInfo(Pics[index]);
                    bitmapSource ??= await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false) ??
                                     ImageFunctions.ImageErrorMessage();
                    preloadValue.BitmapSource = bitmapSource;
                    preloadValue.IsLoading = false;
                    preloadValue.FileInfo = fileInfo;

#if DEBUG
                    if (ShowAddRemove)
                        Trace.WriteLine($"{fileInfo.Name} added at {index}");
#endif
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(AddAsync)} exception: \n{ex}");
#endif
            }
        }

        /// <summary>
        ///  Renames the key of a specified file in the cache.
        /// </summary>
        /// <param name="index">index of file to be renamed</param>
        /// <returns></returns>
        internal static void Rename(int index)
        {
            if (index < 0 || index >= Pics.Count) { return; }

            PreloadList.TryRemove(index, out var preloadValue);
            preloadValue.FileInfo = null;
            _= AddAsync(index, null, preloadValue.BitmapSource).ConfigureAwait(true);
        }

        /// <summary>
        /// Removes all keys from the cache.
        /// </summary>
        internal static void Clear()
        {
            PreloadList.Clear();
        }

        /// <summary>
        /// Returns the specified BitmapSource.
        /// Returns null if key not found.
        /// </summary>
        /// <param name="key">The corresponding filename</param>
        /// <returns></returns>
        internal static PreloadValue? Get(int key)
        {
            return PreloadList.TryGetValue(key, out var preloadValue) ? preloadValue : null;
        }

        /// <summary>
        /// Removes the key with the specified index, after checking if it exists
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(int key)
        {
            if (key >= Pics?.Count || key < 0)
                return;

            if (!PreloadList.ContainsKey(key))
                return;

            try
            {
                _ = PreloadList[key];
                var remove = PreloadList.TryRemove(key, out _);
#if DEBUG
                if (remove && ShowAddRemove)
                    Trace.WriteLine($"{Pics[key]} removed at {Pics.IndexOf(Pics[key])}");
#endif
            }

            catch (Exception e)
            {
#if DEBUG
                    Trace.WriteLine($"{nameof(Remove)} exception:\n{e.Message}");
#endif
            }
        }

        /// <summary>
        /// An asynchronous task that iterates through filenames and caches the decoded images to the preload list
        /// </summary>
        /// <param name="currentIndex">The starting point for the iteration.</param>
        internal static Task PreLoadAsync(int currentIndex) => Task.Run(() =>
        {
            int nextStartingIndex, prevStartingIndex, deleteIndex;

#if DEBUG
            if (ShowAddRemove)
                Trace.WriteLine($"\nPreloading started at {currentIndex}\n");
#endif
            if (!Reverse)
            {
                nextStartingIndex = currentIndex;
                prevStartingIndex = currentIndex - 1;
                deleteIndex = prevStartingIndex - NegativeIterations;

                Parallel.For(0, PositiveIterations, i =>
                {
                    int index = (nextStartingIndex + i) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                Parallel.For(0, NegativeIterations, i =>
                {
                    int index = (prevStartingIndex - i + Pics.Count) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                if (Pics.Count > MaxCount + NegativeIterations)
                {
                    for (int i = 0; i < NegativeIterations; i++)
                    {
                        int index = (deleteIndex - i + Pics.Count) % Pics.Count;
                        Remove(index);
                    }
                }
            }
            else
            {
                nextStartingIndex = currentIndex;
                prevStartingIndex = currentIndex + 1;
                deleteIndex = prevStartingIndex + NegativeIterations;

                Parallel.For(0, PositiveIterations, i =>
                {
                    int index = (nextStartingIndex - i + Pics.Count) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                Parallel.For(0, NegativeIterations, i =>
                {
                    int index = (prevStartingIndex + i) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                if (Pics.Count > MaxCount + NegativeIterations)
                {
                    for (int i = 0; i < NegativeIterations; i++)
                    {
                        int index = (deleteIndex + i) % Pics.Count;
                        Remove(index);
                    }
                }
            }

            while (PreloadList.Count > MaxCount)
            {
                try
                {
                    Remove(PreloadList.Keys.First());
                }
                catch (Exception e)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(PreLoadAsync)} exception:\n{e.Message}");
#endif
                }
            }
        });
    }
}