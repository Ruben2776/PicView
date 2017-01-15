using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.lib
{
    internal static class Preloader
    {
        private static readonly ConcurrentDictionary<string, BitmapSource> Sources = new ConcurrentDictionary<string, BitmapSource>();

        internal static void Add(string file)
        {
            if (Contains(file))
                return;
            var ext = Path.GetExtension(file);
            if (ext == ".gif")
                return;
            var pic = ImageManager.RenderToBitmapSource(file, ext);
            if (pic == null)
                return;
            pic.Freeze();
            Sources.TryAdd(file, pic);
        }

        internal static BitmapSource Load(string file)
        {
            if (!Contains(file))
                return null;

            return Sources[file];
        }

        internal static void Add(int i)
        {
            if (i >= MainWindow.Pics.Count || i < 0)
                return;

            if (File.Exists(MainWindow.Pics[i]))
            {
                if (!Contains(MainWindow.Pics[i]))
                    Add(MainWindow.Pics[i]);
            }
            else
                MainWindow.Pics.Remove(MainWindow.Pics[i]);
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

        internal static bool Contains(string key)
        {
            return Sources.ContainsKey(key);
        }

        internal static void Remove(string key)
        {
            if (key == null) return;
            if (!Contains(key)) return;

            var value = Sources[key];
            Sources.TryRemove(key, out value);
            value = null;
        }

        internal static void Clear()
        {
            var array = Sources.Keys.ToArray();

            var timer = new DispatcherTimer
            (
                TimeSpan.FromSeconds(40), DispatcherPriority.ContextIdle, (s, e) => {
                    for (int i = 0; i < array.Length; i++)
                    {
                        Remove(array[i]);
                    }
                    //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                },
                Application.Current.Dispatcher
            );
            timer.Start();
        }

        internal static void Clear(string[] array)
        {
            var timer = new DispatcherTimer
            (
                TimeSpan.FromMinutes(1.5), DispatcherPriority.ContextIdle, (s, e) =>
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        Remove(array[i]);
                    }
                    //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                },
                Application.Current.Dispatcher
            );
            timer.Start();
        }

        internal static int Count()
        {
            return Sources.Count;
        }
    }

}
