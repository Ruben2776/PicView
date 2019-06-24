using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;
using static PicView.lib.FileFunctions;
using static PicView.lib.Variables;

namespace PicView.lib
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

        internal static void Add(string file)
        {
            if (Contains(file))
                return;

            var pic = ImageManager.RenderToBitmapSource(file, Path.GetExtension(file));
            if (pic == null)
                return;
            pic.Freeze();
            Sources.TryAdd(file, pic);
        }

        internal static void Add(int i)
        {
            if (i >= Pics.Count || i < 0)
                return;

            if (File.Exists(Pics[i]))
            {
                if (!Contains(Pics[i]))
                    Add(Pics[i]);
            }
            else
                Pics.Remove(Pics[i]);
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

            var value = Sources[key];
            Sources.TryRemove(key, out value);
            value = null;
        }

        /// <summary>
        /// Removes all keys and clears them when app is idle
        /// </summary>
        internal static void Clear()
        {
            // Add elemnts to Clear method and set timer to fast
            Clear(Sources.Keys.ToArray(), true);
        }

        /// <summary>
        /// Removes specific keys and clears them when app is idle
        /// </summary>
        /// <param name="array"></param>
        internal static async void Clear(string[] array, bool fast = false)
        {
            // Set time to clear the images
            var timeInSeconds = 120;
            
            // clear faster if it contains a lot of images or if fast == true
            if (Sources.Count > 100)
            {
                timeInSeconds = 45;
            }
            else if (fast)
            {
                timeInSeconds = 20;
            }

            await Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(timeInSeconds));

                // Remove elements
                for (int i = 0; i < array.Length; i++)
                {
                    Remove(array[i]);
                }
                GC.Collect();

                if (!fast)
                {
                    var timer = new DispatcherTimer
                    (
                        TimeSpan.FromSeconds(timeInSeconds), DispatcherPriority.Background, (s, e) =>
                        {
                            var window = Application.Current.MainWindow as MainWindow;
                            window.Reload();
                        },
                        Application.Current.Dispatcher
                    );
                    timer.Start();
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
        internal static void PreLoad(int index, bool reverse)
        {
            if (!reverse)
            {
                //Add first three
                var i = index + 1 >= Pics.Count ? (index + 1) - Pics.Count : index + 1;
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

                if (!freshStartup)
                {
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
            }

            else
            {
                //Add first three
                var i = index - 1 < 0 ? Pics.Count : index - 1;
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

                if (!freshStartup)
                {
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
            // If very large archive being extracted, update Pics
            if (!string.IsNullOrWhiteSpace(TempZipPath) && index >= 5 && Pics.Count > 10)
            {
                if (!Directory.Exists(TempZipPath))
                    return;

                var getProcesses = Process.GetProcessesByName("7z");
                if (getProcesses.Length > 0)
                    Pics = FileList(TempZipPath);

                getProcesses = Process.GetProcessesByName("Zip");
                if (getProcesses.Length > 0)
                    Pics = FileList(TempZipPath);
            }
        }
        
    }

}
