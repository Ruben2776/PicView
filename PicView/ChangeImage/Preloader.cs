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
        /// Set the maximum number of bitmaps to be cached
        /// </summary>
        internal const int MaxCount = 20;

        /// <summary>
        /// Keep track of file names to delete oldest values
        /// </summary>
        private static readonly Queue<string> _keys = new Queue<string>();

        /// <summary>
        /// Adds a file to the preloader from the specified index. Returns true if a new value was added.
        /// </summary>
        /// <param name="i">Index of the image in the list of Pics</param>
        /// <param name="fileInfo">The file info of the image</param>
        /// <param name="bitmapSource">The bitmap source of the image</param>
        /// <returns>Preloadvalue that can be null</returns>
        internal static async Task<PreloadValue?> AddAsync(int index, FileInfo? fileInfo = null, BitmapSource? bitmapSource = null)
        {
            if (index < 0 || index >= Pics.Count) return null;

            if (_preloadList.ContainsKey(Pics[index]))
                return Preloader.Get(Pics[index]);

            try
            {
                _keys.Enqueue(Pics[index]);
                if (_keys.Count > MaxCount)
                {
                    var oldestKey = _keys.Dequeue();
                    _preloadList.TryRemove(oldestKey, out _);
                }

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
                    return preloadValue;
                }
                else
                {
                    return Preloader.Get(Pics[index]);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(AddAsync)} exception: \n {ex}");
#endif
                return null;
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
            _keys.Clear();
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
        /// An asynchronous task that iterates through filenames and caches the decoded images to the preload list
        /// </summary>
        /// <param name="currentIndex">The starting point for the iteration.</param>
        internal static Task PreLoadAsync(int currentIndex) => Task.Run(() =>
        {
            int nextStartingIndex, prevStartingIndex;
            int positiveIterations = 6;
            int negativeIterations = 3;

            if (!Reverse)
            {
                nextStartingIndex = currentIndex;
                prevStartingIndex = currentIndex - 1;

                Parallel.For(0, positiveIterations, i =>
                {
                    int index = (nextStartingIndex + i) % Pics.Count;
                    _= AddAsync(index).ConfigureAwait(false);
                });
                Parallel.For(0, negativeIterations, i =>
                {
                    int index = (prevStartingIndex - i + Pics.Count) % Pics.Count;
                    _= AddAsync(index).ConfigureAwait(false);
                });
            }
            else
            {
                nextStartingIndex = currentIndex;
                prevStartingIndex = currentIndex + 1;

                Parallel.For(0, positiveIterations, i =>
                {
                    int index = (nextStartingIndex - i + Pics.Count) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
                Parallel.For(0, negativeIterations, i =>
                {
                    int index = (nextStartingIndex - i + Pics.Count) % Pics.Count;
                    _ = AddAsync(index).ConfigureAwait(false);
                });
            }
        });
    }
}