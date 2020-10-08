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
            internal BitmapSource bitmapSource;
            internal bool isLoading;

            internal PreloadValue(BitmapSource bitmap, bool loading)
            {
                bitmapSource = bitmap;
                isLoading = loading;
            }
        }

        /// <summary>
        /// Preloader list of BitmapSources
        /// </summary>
        private static readonly ConcurrentDictionary<
            string, PreloadValue> Sources = new ConcurrentDictionary<string, PreloadValue>();

        /// <summary>
        /// Add file to preloader
        /// </summary>
        /// <param name="file">file path</param>
        internal static Task Add(string file) => Task.Run(async () =>
        {
            // Add bitmapsource as null and set loading to true, to avoid consecutively adding value
            var preloadValue = new PreloadValue(null, true);
            if (Sources.TryAdd(file, preloadValue))
            {
                var x = await ImageDecoder.RenderToBitmapSource(file).ConfigureAwait(false);
                preloadValue.bitmapSource = x;
                preloadValue.isLoading = false;
            }
        });

        /// <summary>
        /// Add file to preloader from index
        /// </summary>
        /// <param name="i">Index of Pics</param>
        internal static void Add(int i)
        {
            if (i >= Pics.Count || i < 0)
            {
                return;
            }

            Add(Pics[i]);
        }

        /// <summary>
        /// Removes the key, after checking if it exists
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(string key)
        {
            if (key == null)
            {
#if DEBUG
                Trace.WriteLine("Preloader.Remove key null, " + key);
#endif
                return;
            }

            if (!Contains(key))
            {
#if DEBUG
                Trace.WriteLine("Preloader.Remove does not contain " + key);
#endif
                return;
            }

            _ = Sources[key];
#if DEBUG
            if (!Sources.TryRemove(key, out _))
            {
                Trace.WriteLine($"Failed to Remove {key} from Preloader, index {Pics.IndexOf(key)}");
            }
#else
            Sources.TryRemove(key, out _);
#endif
        }

        /// <summary>
        /// Removes the key, after checking if it exists
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(int i)
        {
            if (i >= Pics.Count || i < 0)
            {
                return;
            }

            Remove(Pics[i]);
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
        internal static PreloadValue Get(string key)
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
            if (string.IsNullOrWhiteSpace(key) || Sources.IsEmpty)
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
            var loadInfront = 2;
            var loadBehind = 1;

            if (Reverse)
            {
                // Add first elements behind
                int y = 0;
                for (int i = index - 1; i > index - 1 - loadInfront; i--)
                {
                    y++;
                    if (i < 0)
                    {
                        for (int x = Pics.Count - 1; x >= Pics.Count - y; x--)
                        {
                            Add(x);
                        }
                        break;
                    }
                    Add(i);
                }

                // Add second elements
                for (int i = index + 1; i <= index + 1 + loadInfront; i++)
                {
                    if (i != 0 || Pics.Count != 0)
                    {
                        Add(i % Pics.Count);
                    }
                }

                //Clean up infront
                if (Pics.Count > loadInfront + loadInfront && !FreshStartup)
                {
                    for (int i = index + 1 + loadInfront; i > index + 1 + loadInfront - loadBehind; i--)
                    {
                        if (i != 0 || Pics.Count != 0)
                        {
                            Remove(i % Pics.Count);
                        }
                    }
                }
            }
            else
            {
                // Add first elements
                for (int i = index + 1; i < index + 1 + loadInfront; i++)
                {
                    if (i != 0 || Pics.Count != 0)
                    {
                        Add(i % Pics.Count);
                    }
                }
                // Add second elements behind
                for (int i = index - 1; i > index - 1 - loadBehind; i--)
                {
                    if (i != 0 || Pics.Count != 0)
                    {
                        Add(i % Pics.Count);
                    }
                }

                //Clean up behind
                if (Pics.Count > loadInfront * 2 && !FreshStartup)
                {
                    for (int i = index - 1 - loadInfront; i < (index - 1 - loadBehind); i++)
                    {
                        Remove(i % Pics.Count);
                    }
                }
            }
        });
    }
}