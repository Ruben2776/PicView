using PicView.ImageHandling;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;

namespace PicView.ChangeImage
{
    internal static class Preloader
    {
        internal class PreloadValue
        {
            /// <summary>
            /// The bitmap source of the image
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
        private static readonly ConcurrentDictionary<string, PreloadValue> _preloadList = new();

        /// <summary>
        /// Sets the number of iterations to load infront 
        /// </summary>
        const int positiveIterations = 8;

        /// <summary>
        /// Sets the number of iterations to load behind
        /// </summary>
        const int negativeIterations = 4;

        /// <summary>
        /// Set the maximum number of bitmaps to be cached
        /// </summary>
        internal const int MaxCount = positiveIterations + negativeIterations;

        /// <summary>
        /// Adds a file to the preloader from the specified index. Returns true if a new value was added.
        /// </summary>
        /// <param name="i">Index of the image in the list of Pics</param>
        /// <param name="fileInfo">The file info of the image</param>
        /// <param name="bitmapSource">The bitmap source of the image</param>
        /// <returns>Preloadvalue that can be null</returns>
        internal static async Task AddAsync(int index, FileInfo? fileInfo = null, BitmapSource? bitmapSource = null)
        {
            if (index < 0 || index >= Pics.Count) return;

            try
            {
                var preloadValue = new PreloadValue(null, true, null);
                var add = _preloadList.TryAdd(Pics[index], preloadValue);
                if (add)
                {
                    fileInfo ??= new FileInfo(Pics[index]);
                    bitmapSource = bitmapSource ??
                        await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false) ??
                        ImageFunctions.ImageErrorMessage();
                    preloadValue.BitmapSource = bitmapSource;
                    preloadValue.IsLoading = false;
                    preloadValue.FileInfo = fileInfo;

#if DEBUG
                    Trace.WriteLine($"{fileInfo.Name} added at {index}");
#endif
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(AddAsync)} exception: \n {ex}");
#endif
            }
        }

        /// <summary>
        ///  Renames the key of a specified file in the cache.
        /// </summary>
        /// <param name="file">File to be renamed</param>
        /// <param name="name">New name to be changed to</param>
        /// <returns></returns>
        internal static void Rename(string file, int index, string name)
        {
            if (file == null || name == null) { return; }
            if (index < 0 || index >= Pics.Count) { return; }

            _preloadList.Remove(file, out var preloadValue);
            _= AddAsync(index, null, preloadValue.BitmapSource).ConfigureAwait(true);
        }

        /// <summary>
        /// Removes all keys from the cache.
        /// </summary>
        internal static void Clear()
        {
            _preloadList.Clear();
        }

        /// <summary>
        /// Returns the specified BitmapSource.
        /// Returns null if key not found.
        /// </summary>
        /// <param name="key">The corrosponding filename</param>
        /// <returns></returns>
        internal static PreloadValue? Get(string key)
        {
            if (_preloadList.TryGetValue(key, out var preloadValue))
            {
                return preloadValue;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Removes the key with the specified index, after checking if it exists
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(int key)
        {
            if (key >= Pics?.Count || key < 0)
                return;

            if (!_preloadList.ContainsKey(Pics[key]))
                return;

            try
            {
                _ = _preloadList[Pics[key]];
                var remove = _preloadList.TryRemove(Pics[key], out _);
#if DEBUG
                if (remove)
                    Trace.WriteLine($"{Pics[key]} removed at {Pics.IndexOf(Pics[key])}");
#endif
            }

            catch (Exception e)
            {
#if DEBUG
                    Trace.WriteLine($"{nameof(Remove)} exception:\n{e.Message}");
#endif

                return;
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
            Trace.WriteLine($"\nPreloading started at {currentIndex} \n");
#endif
            if (!Reverse)
            {
                nextStartingIndex = currentIndex;
                prevStartingIndex = currentIndex - 1;
                deleteIndex = prevStartingIndex - negativeIterations;

                Parallel.For(0, positiveIterations, i =>
                {
                    int index = (nextStartingIndex + i) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                Parallel.For(0, negativeIterations, i =>
                {
                    int index = (prevStartingIndex - i + Pics.Count) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                for (int i = 0; i < negativeIterations; i++)
                {
                    int index = (deleteIndex - i + Pics.Count) % Pics.Count;
                    Remove(index);
                }
            }
            else
            {
                nextStartingIndex = currentIndex;
                prevStartingIndex = currentIndex + 1;
                deleteIndex = prevStartingIndex + positiveIterations;

                Parallel.For(0, positiveIterations, i =>
                {
                    int index = (nextStartingIndex - i + Pics.Count) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                Parallel.For(0, negativeIterations, i =>
                {
                    int index = (prevStartingIndex + i) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                for (int i = 0; i < negativeIterations; i++)
                {
                    int index = (deleteIndex + i) % Pics.Count;
                    Remove(index);
                }
            }
        });
    }
}