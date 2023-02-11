using PicView.ImageHandling;
using PicView.Properties;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;

namespace PicView.ChangeImage
{
    /// <summary>
    /// Used for containing a list of BitmapSources
    /// </summary>
    internal static class Preloader
    {
        internal class PreloadValue
        {
            internal BitmapSource? BitmapSource;
            internal bool IsLoading;
            internal FileInfo? FileInfo;

            internal PreloadValue(BitmapSource? bitmap, bool loading, FileInfo? fileInfo)
            {
                BitmapSource = bitmap;
                IsLoading = loading;
                FileInfo = fileInfo;
            }
        }

        /// <summary>
        /// Preloader list of BitmapSources
        /// </summary>
        private static readonly ConcurrentDictionary<string, PreloadValue> _preloadList = new();

        /// <summary>
        /// Add file to preloader from index. Returns true if new value added.
        /// </summary>
        /// <param name="i">Index of Pics</param>
        /// <param name="fileInfo"></param>
        /// <param name="bitmapSource"></param>
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

            var preloadValue = new PreloadValue(null, true, null);
            _preloadList.TryAdd(Pics[index], preloadValue);

            try
            {
                fileInfo = fileInfo ?? new FileInfo(Pics[index]);
                bitmapSource = bitmapSource ?? await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
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
        /// Removes the key, after checking if it exists
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(int key)
        {
            if (key < 0) // Make it load at start of folder, when looping is on
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

        internal static bool Rename(string file, string name)
        {
            if (file == null || name == null) { return false; }

            return _preloadList.TryRemove(file, out var preloadValue) && _preloadList.TryAdd(name, preloadValue);
        }

        /// <summary>
        /// Removes all keys
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
        /// <param name="key"></param>
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
        /// <param name="key"></param>
        /// <returns></returns>
        internal static bool Contains(string key)
        {
            return !_preloadList.IsEmpty && _preloadList.ContainsKey(key);
        }


        /// <summary>
        /// Starts decoding images into memory,
        /// based on current index and if reverse or not
        /// </summary>
        /// <param name="index"></param>
        /// <param name="reverse"></param>
        internal static Task PreLoadAsync(int currentIndex) => Task.Run(async () =>
        {
            int loadInfront = Pics.Count >= 10 ? 5 : 3;
            int loadBehind = Pics.Count >= 10 ? 3 : 2;

            int endPoint;
            if (Reverse)
            {
                endPoint = currentIndex - 1 - loadInfront;
                // Add first elements behind
                for (int i = currentIndex - 1; i > endPoint; i--)
                {
                    if (Pics.Count == 0 || Pics.Count == _preloadList.Count) { return; }
                    await AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                // Add second elements
                for (int i = currentIndex + 1; i < (currentIndex + 1) + loadBehind; i++)
                {
                    if (Pics.Count == 0 || Pics.Count == _preloadList.Count) { return; }
                    await AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                //Clean up infront
                endPoint = endPoint + loadInfront;
                for (int i = (currentIndex + 1) + loadBehind; i < (currentIndex + 1) + endPoint; i++)
                {
                    if (Pics.Count == 0 || Pics.Count == _preloadList.Count) { return; }
                    Remove(i % Pics.Count);
                }
            }
            else
            {
                endPoint = (currentIndex - 1) - loadBehind;
                // Add first elements
                for (int i = currentIndex + 1; i < (currentIndex + 1) + loadInfront; i++)
                {
                    if (Pics.Count == 0 || Pics.Count == _preloadList.Count) { return; }
                    await AddAsync(i % Pics.Count).ConfigureAwait(false);
                }
                // Add second elements behind
                for (int i = currentIndex - 1; i > endPoint; i--)
                {
                    if (Pics.Count == 0 || Pics.Count == _preloadList.Count) { return; }
                    await AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                //Clean up behind
                endPoint = endPoint + loadBehind;             
                for (int i = currentIndex - loadInfront; i <= endPoint; i++)
                {
                    if (Pics.Count == 0 || Pics.Count == _preloadList.Count) { return; }
                    Remove(i % Pics.Count);
                }
            }
        });

    }
}