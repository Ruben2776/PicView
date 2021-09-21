using PicView.ImageHandling;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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

            internal PreloadValue(BitmapSource? bitmap, bool loading)
            {
                bitmapSource = bitmap;
                isLoading = loading;
            }
        }

        /// <summary>
        /// Preloader list of BitmapSources
        /// </summary>
        private static readonly ConcurrentDictionary<
            int, PreloadValue> Sources = new ConcurrentDictionary<int, PreloadValue>();

        internal const int LoadInfront = 4;
        internal const int LoadBehind = 2;

        /// <summary>
        /// Add file to preloader from index
        /// </summary>
        /// <param name="i">Index of Pics</param>
        internal static async Task AddAsync(int i)
        {
            if (Pics == null || i >= Pics?.Count)
            {
                return;
            }

            if (i < 0)
            {
                i = Math.Abs(i);
            }

            var preloadValue = new PreloadValue(null, true);
            if (Sources.TryAdd(i, preloadValue))
            {
                var x = await ImageDecoder.RenderToBitmapSource(Pics?[i]).ConfigureAwait(false);
                preloadValue.bitmapSource = x;
                preloadValue.isLoading = false;
            }
        }

        /// <summary>
        /// Get count of preload values
        /// </summary>
        /// <returns></returns>
        internal static int Count()
        {
            return Sources.Count;
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
                Trace.WriteLine("Preloader.Remove key null, " + key);
#endif
                return;
            }

            if (!Contains(key))
            {
                return;
            }

            _ = Sources[key];
#if DEBUG
            if (!Sources.TryRemove(key, out _))
            {
                Trace.WriteLine($"Failed to Remove {key} from Preloader, index {Pics?[key]}");
            }
#else
            Sources.TryRemove(key, out _);
#endif
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
        internal static PreloadValue? Get(int key)
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
        internal static bool Contains(int key)
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
        internal static Task PreLoad(int index) => Task.Run(async () =>
        {
            int endPoint;
            if (Reverse)
            {
                endPoint = index - 1 - LoadInfront;
                // Add first elements behind
                for (int i = index - 1; i > endPoint; i--)
                {
                    await AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                // Add second elements
                for (int i = index + 1; i < (index + 1) + LoadBehind; i++)
                {
                    await AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                //Clean up infront
                for (int i = (index + 1) + LoadBehind; i < ((index + 1) + LoadInfront * 2); i++)
                {
                    Remove(i % Pics.Count);
                }
            }
            else
            {
                endPoint = (index - 1) - LoadBehind;
                // Add first elements
                for (int i = index + 1; i < (index + 1) + LoadInfront; i++)
                {
                    await AddAsync(i % Pics.Count).ConfigureAwait(false);
                }
                // Add second elements behind
                for (int i = index - 1; i > endPoint; i--)
                {
                    await AddAsync(i % Pics.Count).ConfigureAwait(false);
                }

                //Clean up behind
                for (int i = index - LoadInfront * 2; i <= endPoint; i++)
                {
                    Remove(i % Pics.Count);
                }
            }
        });
    }
}