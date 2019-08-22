using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static PicView.FileLists;
using static PicView.Fields;

namespace PicView.PreLoading
{
    /// <summary>
    /// Used for containing a list of BitmapSources
    /// </summary>
    internal static class Preloader
    {
        /// <summary>
        /// Contains A list of BitmapSources
        /// </summary>
        private static readonly ConcurrentDictionary<string, BitmapSource> Sources = new ConcurrentDictionary<string, BitmapSource>();

        internal static bool IsLoading;

        internal static void Add(string file)
        {
            if (Contains(file))
                return;

            IsLoading = true;

            var pic = ImageManager.RenderToBitmapSource(file);
            if (pic == null)
            {
                IsLoading = false;
                return;
            }
            pic.Freeze();
            Sources.TryAdd(file, pic);
            IsLoading = false;
        }

        internal static void Add(int i)
        {
            if (i >= Pics.Count || i < 0)
                return;

            IsLoading = true;

            //#if DEBUG
            //System.Threading.Thread.Sleep(500);
            //#endif

            if (File.Exists(Pics[i]))
            {
                if (!Contains(Pics[i]))
                    Add(Pics[i]);
            }
            else
                Pics.Remove(Pics[i]);

            IsLoading = false;
        }

        internal static void Add(BitmapSource bmp, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            if (Contains(key))
                return;
            if (bmp == null)
                return;
            if (!bmp.IsFrozen)
                bmp.Freeze();
            Sources.TryAdd(key, bmp);
        }

        /// <summary>
        /// Removes the key, after checking if it exists
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(string key)
        {
            if (key == null) return;
            if (!Contains(key)) return;
            _ = Sources[key];
            Sources.TryRemove(key, out _);
        }

        /// <summary>
        /// Removes all keys and clears them when app is idle
        /// </summary>
        internal static Task Clear()
        {
            // Add elemnts to Clear method and set timer to fast
            return Clear(Sources.Keys.ToArray(), true);
        }

        /// <summary>
        /// Removes specific keys and clears them when app is idle
        /// </summary>
        /// <param name="array"></param>
        internal static Task Clear(string[] array, bool fast = false)
        {
            // Set time to clear the images
            var timeInSeconds = 40;

            // clear faster if it contains a lot of images or if fast == true
            if (Sources.Count > 50)
            {
                timeInSeconds = 15;
            }
            else if (fast)
            {
                timeInSeconds = 20;
            }

            #if DEBUG
            timeInSeconds = 15;
            #endif


            return Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(timeInSeconds));

                // Remove elements
                for (int i = 0; i < array.Length; i++)
                {
                    Remove(array[i]);
                    GC.Collect();
                }
            });
        }

        /// <summary>
        /// Returns the specified BitmapSource.
        /// Returns null if key not found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static BitmapSource Load(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !Contains(key))
                return null;

            return Sources[key];
        }

        /// <summary>
        /// Checks if the specified key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static bool Contains(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;
            return Sources.ContainsKey(key);
        }

        internal static int Count { get { return Sources.Count; } }

        /// <summary>
        /// Starts decoding images into memory,
        /// based on current index and if reverse or not
        /// </summary>
        /// <param name="index"></param>
        /// <param name="reverse"></param>
        internal static Task PreLoad(int index, bool reverse)
        {
            return Task.Run(() =>
            {
                int i;

                if (!Properties.Settings.Default.Looping)
                {
                    if (!reverse && index < Pics.Count)
                    {
                        //Add first three
                        i = index + 1 >= Pics.Count ? Pics.Count : index + 1;
                        Add(i);
                        i += 1 >= Pics.Count ? Pics.Count - 1 : i += 1;
                        Add(i);
                        i += 1 >= Pics.Count ? Pics.Count - 1 : i += 1;
                        Add(i);

                        //Add two behind
                        i = index - 1 < 0 ? 0 : index - 1;
                        Add(i);
                        i = i - 1 < 0 ? 0 : i - 1;
                        Add(i);

                        //Add one more infront
                        i = index + 4 >= Pics.Count ? Pics.Count : index + 4;
                        Add(i);

                        //Clean up behind
                        var arr = new string[3];
                        i = index - 3 < 0 ? (Pics.Count - index) - 3 : index - 3;
                        if (i > -1 && i < Pics.Count)
                            arr[0] = Pics[i];
                        i = i - 1 < 0 ? (Pics.Count - index) - 1 : i - 1;
                        if (i > -1 && i < Pics.Count)
                            arr[1] = Pics[i];
                        i = i - 1 < 0 ? (Pics.Count - index) - 1 : i - 1;
                        if (i > -1 && i < Pics.Count)
                            arr[2] = Pics[i];
                        Clear(arr);
                    }
                    else
                    {
                        //Add first three
                        i = index - 1 < 0 ? Pics.Count - index : index - 1;
                        Add(i);
                        i = i - 1 < 0 ? 0 : i - 1;
                        Add(i);
                        i = i - 1 < 0 ? 0 : i - 1;
                        Add(i);

                        //Add two infront
                        i = index + 1 >= Pics.Count ? Pics.Count : index + 1;
                        Add(i);
                        i = i + 1 >= Pics.Count ? (i + 1) - Pics.Count : i + 1;
                        Add(i);

                        //Add one more behind
                        i = index - 4 < 0 ? (index + 4) - Pics.Count : index - 4;
                        Add(i);

                        //Clean up behind
                        var arr = new string[3];
                        i = index + 3 > Pics.Count - 1 ? Pics.Count - 1 : index + 3;
                        arr[0] = Pics[i];
                        i = index + 4 > Pics.Count - 1 ? Pics.Count - 1 : index + 4;
                        arr[1] = Pics[i];
                        i = index + 5 > Pics.Count - 1 ? Pics.Count - 1 : index + 5;
                        arr[2] = Pics[i];
                        Clear(arr);

                    }
                }
                else
                {
                    if (!reverse)
                    {
                        //Add first three
                        i = index + 1 >= Pics.Count ? (index + 1) - Pics.Count : index + 1;
                        Add(i);
                        i = i + 1 >= Pics.Count ? (i + 1) - Pics.Count : i + 1;
                        Add(i);
                        i = i + 1 >= Pics.Count ? (i + 1) - Pics.Count : i + 1;
                        Add(i);

                        //Add two behind, but nof if just started
                        if (!freshStartup)
                        {
                            i = index - 1 < 0 ? Pics.Count - index : index - 1;
                            Add(i);
                            i = i - 1 < 0 ? Pics.Count - i : i - 1;
                            Add(i);
                        }
                        //Add one more infront
                        i = index + 4 >= Pics.Count ? (index + 4) - Pics.Count : index + 4;
                        Add(i);

                        //Clean up behind
                        var arr = new string[3];
                        i = index - 3 < 0 ? (Pics.Count - index) - 3 : index - 3;
                        if (i > -1 && i < Pics.Count)
                            arr[0] = Pics[i];
                        i = i - 1 < 0 ? (Pics.Count - index) - 1 : i - 1;
                        if (i > -1 && i < Pics.Count)
                            arr[1] = Pics[i];
                        i = i - 1 < 0 ? (Pics.Count - index) - 1 : i - 1;
                        if (i > -1 && i < Pics.Count)
                            arr[2] = Pics[i];
                        Clear(arr);
                    }

                    else
                    {
                        //Add first three
                        i = index - 1 < 0 ? Pics.Count : index - 1;
                        Add(i);
                        i = i - 1 < 0 ? Pics.Count : i - 1;
                        Add(i);
                        i = i - 1 < 0 ? Pics.Count : i - 1;
                        Add(i);

                        //Add two behind
                        i = index + 1 >= Pics.Count ? (i + 1) - Pics.Count : index + 1;
                        Add(i);
                        i = i + 1 >= Pics.Count ? (i + 1) - Pics.Count : i + 1;
                        Add(i);

                        //Add one more infront
                        i = index - 4 < 0 ? (index + 4) - Pics.Count : index - 4;
                        Add(i);

                        //Clean up behind
                        var arr = new string[3];
                        i = index + 3 > Pics.Count - 1 ? Pics.Count - 1 : index + 3;
                        arr[0] = Pics[i];
                        i = index + 4 > Pics.Count - 1 ? Pics.Count - 1 : index + 4;
                        arr[1] = Pics[i];
                        i = index + 5 > Pics.Count - 1 ? Pics.Count - 1 : index + 5;
                        arr[2] = Pics[i];
                        Clear(arr);
                    }
                }

                // Update Pics if needed
                var tmp = FileList(Path.GetDirectoryName(PicPath));
                if (tmp.Count != Pics.Count)
                    Pics = tmp;
            });
        }
    }

}
