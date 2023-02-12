using PicView.ImageHandling;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
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
        /// A concurrent dictionary containing the preloaded images
        /// </summary>
        private static readonly ConcurrentDictionary<string, PreloadValue> _preloadList = new();

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
#if DEBUG
                if (!_preloadList.TryAdd(Pics[index], preloadValue))
                {
                    Trace.WriteLine($"Failed to Remove {Pics[index]} from Preloader, index {Pics?[index]}");
                }
#else
                _preloadList.TryAdd(Pics[index], preloadValue);
#endif

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
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
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
#if DEBUG
                Trace.WriteLine("Preloader " + nameof(Remove) + " key null, " + key);
#endif
                return;
            }

            if (!Contains(Pics[key]))
            {
                return;
            }

            try
            {
                _ = _preloadList[Pics[key]];
#if DEBUG
                if (!_preloadList.TryRemove(Pics[key], out _))
                {
                    Trace.WriteLine($"Failed to Remove {key} from Preloader, index {Pics?[key]}");
                }
#else
                _preloadList.TryRemove(Navigation.Pics[key], out _);
#endif
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("Preloader " + nameof(Remove) + "exception" + Environment.NewLine + e.Message);
            }
#else
            catch (Exception)
            {
                return;
            }
#endif
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

            return _preloadList.TryRemove(file, out var preloadValue) && _preloadList.TryAdd(name, preloadValue);
        }

        /// <summary>
        /// Removes all keys from the cache.
        /// </summary>
        internal static void Clear()
        {
            if (_preloadList.IsEmpty)
            {
                return;
            }

            _preloadList.Clear();
#if DEBUG
            Trace.WriteLine("Cleared Preloader");
#endif
        }

        /// <summary>
        /// Returns the specified BitmapSource.
        /// Returns null if key not found.
        /// </summary>
        /// <param name="key">The corrosponding filename</param>
        /// <returns></returns>
        internal static PreloadValue? Get(string key)
        {
            if (!Contains(key))
            {
                return null;
            }

            return _preloadList[key];
        }

        /// <summary>
        /// Checks if the specified key exists
        /// </summary>
        /// <param name="key">The corrosponding filename</param>
        /// <returns></returns>
        internal static bool Contains(string key)
        {
            return !_preloadList.IsEmpty && _preloadList.ContainsKey(key);
        }

        /// <summary>
        /// An asynchronous task that iterates through filenames and caches the decoded images to the preload list
        /// </summary>
        /// <param name="currentIndex">The starting point for the iteration.</param>
        internal static Task PreLoadAsync(int currentIndex) => Task.Run(async () =>
        {
            int nextSixStartingIndex, prevThreeStartingIndex;
            int positiveIterations = 6;
            int negativeIterations = 3;

            if (!Reverse)
            {
                nextSixStartingIndex = currentIndex;
                prevThreeStartingIndex = currentIndex - negativeIterations;
                if (prevThreeStartingIndex < 0)
                {
                    prevThreeStartingIndex = Pics.Count - (negativeIterations - currentIndex);
                }
            }
            else
            {
                nextSixStartingIndex = currentIndex - positiveIterations;
                if (nextSixStartingIndex < 0)
                {
                    nextSixStartingIndex = Pics.Count - (positiveIterations - currentIndex);
                }

                prevThreeStartingIndex = currentIndex;
            }

            for (int i = 0; i < positiveIterations; i++)
            {
                int index = (nextSixStartingIndex + i) % Pics.Count;
                await AddAsync(index).ConfigureAwait(false);
            }

            for (int i = 0; i < negativeIterations; i++)
            {
                int index = (prevThreeStartingIndex - i + Pics.Count) % Pics.Count;
                await AddAsync(index).ConfigureAwait(false);
            }

            int removeIndex = (prevThreeStartingIndex - negativeIterations + Pics.Count) % Pics.Count;
            Remove(removeIndex);
        });
    }
}