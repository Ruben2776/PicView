using PicView.ImageHandling;
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
        private static readonly Dictionary<string, PreloadValue> _preloadList = new();

        /// <summary>
        /// Adds a file to the preloader from the specified index. Returns true if a new value was added.
        /// </summary>
        /// <param name="i">Index of the image in the list of Pics</param>
        /// <param name="fileInfo">The file info of the image</param>
        /// <param name="bitmapSource">The bitmap source of the image</param>
        /// <returns>Whether a new value was added</returns>
        internal static async Task<bool> AddAsync(int index, FileInfo? fileInfo = null, BitmapSource? bitmapSource = null)
        {
            if (index < 0 || index >= Pics.Count)
            {
                return false;
            }

            if (_preloadList.ContainsKey(Pics[index]))
            {
                return false;
            }
            try
            {
                var preloadValue = new PreloadValue(null, true, null);
                var add = _preloadList.TryAdd(Pics[index], preloadValue);

                fileInfo = fileInfo ?? new FileInfo(Pics[index]);
                bitmapSource = bitmapSource ?? await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                if (bitmapSource is null)
                {
                    bitmapSource = ImageFunctions.ImageErrorMessage();
                }
                preloadValue.BitmapSource = bitmapSource;
                preloadValue.IsLoading = false;
                preloadValue.FileInfo = fileInfo;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes the key with the specified index, after checking if it exists
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(int key)
        {
            if (key < 0)
            {
                key = Math.Abs(key);
            }
            else if (key >= Pics?.Count)
            {
                return;
            }

            if (!Contains(Pics[key]))
            {
                return;
            }

            try
            {
                _ = _preloadList[Pics[key]];
                _preloadList.Remove(Navigation.Pics[key], out _);
            }

            catch (Exception)
            {
                return;
            }
        }
        /// <summary>
        ///  Renames the key of a specified file in the cache.
        /// </summary>
        /// <param name="file">File to be renamed</param>
        /// <param name="name">New name to be changed to</param>
        /// <returns></returns>
        internal static bool Rename(string file, string name)
        {
            if (file == null || name == null) { return false; }

            return _preloadList.Remove(file, out var preloadValue) && _preloadList.TryAdd(name, preloadValue);
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
        /// Checks if the specified key exists
        /// </summary>
        /// <param name="key">The corrosponding filename</param>
        /// <returns></returns>
        internal static bool Contains(string key)
        {
            return _preloadList.ContainsKey(key);
        }

        /// <summary>
        /// An asynchronous task that iterates through filenames and caches the decoded images to the preload list
        /// </summary>
        /// <param name="currentIndex">The starting point for the iteration.</param>
        internal static Task PreLoadAsync(int currentIndex) => Task.Run(async () =>
        {
            int nextStartingIndex, prevStartingIndex, deleteIndex;
            int positiveIterations = 6;
            int negativeIterations = 3;

            if (!Reverse)
            {
                nextStartingIndex = currentIndex;
                prevStartingIndex = currentIndex - 1;
                deleteIndex = prevStartingIndex - negativeIterations;

                for (int i = 0; i < positiveIterations; i++)
                {
                    int index = (nextStartingIndex + i) % Pics.Count;
                    await AddAsync(index).ConfigureAwait(false);
                }
                for (int i = 0; i < negativeIterations; i++)
                {
                    int index = (prevStartingIndex - i + Pics.Count) % Pics.Count;
                    await AddAsync(index).ConfigureAwait(false);
                }
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

                for (int i = 0; i < positiveIterations; i++)
                {
                    int index = (nextStartingIndex - i + Pics.Count) % Pics.Count;
                    await AddAsync(index).ConfigureAwait(false);
                }
                for (int i = 0; i < negativeIterations; i++)
                {
                    int index = (prevStartingIndex + i) % Pics.Count;
                    await AddAsync(index).ConfigureAwait(false);
                }
                for (int i = 0; i < negativeIterations; i++)
                {
                    int index = (deleteIndex + i) % Pics.Count;
                    Remove(index);
                }
            }
        });
    }
}