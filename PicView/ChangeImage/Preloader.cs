using PicView.ImageHandling;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
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
            internal BitmapSource? bitmapSource;
            internal bool isLoading;
            internal FileInfo? fileInfo;

            internal PreloadValue(BitmapSource? bitmap, bool loading, FileInfo? fileInfo)
            {
                bitmapSource = bitmap;
                isLoading = loading;
                this.fileInfo = fileInfo;
            }
        }

        /// <summary>
        /// Preloader list of BitmapSources
        /// </summary>
        private static readonly ConcurrentDictionary<
            string, PreloadValue> Sources = new ConcurrentDictionary<string, PreloadValue>();

        internal static bool IsRunning { get; private set; }

        /// <summary>
        /// Add file to preloader from index
        /// </summary>
        /// <param name="i">Index of Pics</param>
        internal static async Task AddAsync(int i, FileInfo? fileInfo = null, BitmapSource? bitmapSource = null)
        {
            if (i >= Pics?.Count) { return; }

            if (i < 0)
            {
                i = Math.Abs(i);
            }

            var preloadValue = new PreloadValue(null, true, null);
            if (preloadValue is null) { return; }

            if (Sources.TryAdd(Navigation.Pics[i], preloadValue))
            {
                await Task.Run(async () =>
                {
                    if (fileInfo is null)
                    {
                        fileInfo = new FileInfo(Navigation.Pics[i]);
                    }
                    if (bitmapSource is null)
                    {
                        bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                    }
                    preloadValue.bitmapSource = bitmapSource;
                    preloadValue.isLoading = false;
                    preloadValue.fileInfo = fileInfo;
                }).ConfigureAwait(false);
            }
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

            if (!Contains(Navigation.Pics[key]))
            {
                return;
            }

            try
            {
                _ = Sources[Navigation.Pics[key]];
#if DEBUG
                if (!Sources.TryRemove(Navigation.Pics[key], out _))
                {
                    Trace.WriteLine($"Failed to Remove {key} from Preloader, index {Pics?[key]}");
                }
#else
            Sources.TryRemove(Navigation.Pics[key], out _);
#endif
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("Preloader " + nameof(Remove) + "exception" + Environment.NewLine + e.Message);
                return;
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

            if (Sources.TryRemove(file, out var preloadValue) == false)
            {
                return false;
            }
            if (Sources.TryAdd(name, preloadValue))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes all keys
        /// </summary>
        internal static void Clear()
        {
            if (Sources.IsEmpty)
            {
                return;
            }

            Sources.Clear();
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

            return Sources[key];
        }

        /// <summary>
        /// Checks if the specified key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static bool Contains(string key)
        {
            if (Sources.IsEmpty)
            {
                return false;
            }

            return Sources.ContainsKey(key);
        }

        /// <summary>
        /// Starts decoding images into memory,
        /// based on current index and if reverse or not
        /// </summary>
        /// <param name="index"></param>
        /// <param name="reverse"></param>
        internal static Task PreLoad(int index) => Task.Run(() =>
        {
            IsRunning = true;

            int loadInfront = Pics.Count >= 10 ? 5 : 3;
            int loadBehind = Pics.Count >= 10 ? 3 : 2;

            int endPoint;
            if (Reverse)
            {
                endPoint = index - 1 - loadInfront;
                // Add first elements behind
                for (int i = index - 1; i > endPoint; i--)
                {
                    if (Pics.Count == 0) { return; }
                    _ = AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                // Add second elements
                for (int i = index + 1; i < (index + 1) + loadBehind; i++)
                {
                    if (Pics.Count == 0) { return; }
                    _ = AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                //Clean up infront
                for (int i = (index + 1) + loadBehind; i < (index + 1) + loadInfront; i++)
                {
                    if (Pics.Count == 0) { return; }
                    Remove(i % Pics.Count);
                }
            }
            else
            {
                endPoint = (index - 1) - loadBehind;
                // Add first elements
                for (int i = index + 1; i < (index + 1) + loadInfront; i++)
                {
                    if (Pics.Count == 0) { return; }
                    _ = AddAsync(i % Pics.Count).ConfigureAwait(false);
                }
                // Add second elements behind
                for (int i = index - 1; i > endPoint; i--)
                {
                    if (Pics.Count == 0) { return; }
                    _ = AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                //Clean up behind
                for (int i = index - loadInfront; i <= endPoint; i++)
                {
                    if (Pics.Count == 0) { return; }
                    Remove(i % Pics.Count);
                }
            }

            IsRunning = false;
        });
    }
}