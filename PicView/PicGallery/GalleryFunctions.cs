using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UILogic;
using PicView.Views.UserControls;
using static PicView.UILogic.UC;

namespace PicView.PicGallery
{
    internal static class GalleryFunctions
    {
        internal static bool IsHorizontalOpen { get; set; }
        internal static bool IsVerticalFullscreenOpen { get; set; }
        internal static bool IsHorizontalFullscreenOpen { get; set; }

        public class tempPics
        {
            internal BitmapSource pic;
            public string name;

            public tempPics(BitmapSource pic, string name)
            {
                this.pic = pic;
                this.name = name;
            }
        }

        private static IEnumerable<T> OrderBySequence<T, TId>(this IEnumerable<T> source, IEnumerable<TId> order, Func<T, TId> idSelector)
        {
            var lookup = source?.ToDictionary(idSelector, t => t);
            foreach (var id in order)
            {
                yield return lookup[id];
            }
        }

        internal static async Task SortGallery(FileInfo? fileInfo = null)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (GetPicGallery.Container.Children.Count <= 0)
                {
                }
            }));

            if (fileInfo is null)
            {
                fileInfo = new FileInfo(Navigation.Pics[0]);
            }

            var thumbs = new List<tempPics>();

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                for (int i = 0; i < Navigation.Pics.Count; i++)
                {
                    var x = GetPicGallery.Container.Children[i] as PicGalleryItem;
                    thumbs.Add(new tempPics(x?.img?.Source as BitmapSource, Navigation.Pics[i]));
                }

                Clear();
            }));

            Navigation.Pics.Clear(); // Cancel task if running
            await FileLists.RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

            try
            {
                thumbs = thumbs.OrderBySequence(Navigation.Pics, pic => pic.name).ToList();
            }
            catch (Exception)
            {
                thumbs = null;
                Clear();
                await GalleryLoad.Load().ConfigureAwait(false);
                return;
            }

            for (int i = 0; i < Navigation.Pics.Count; i++)
            {
                GalleryLoad.Add(i, Navigation.FolderIndex);

            }

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                GalleryNavigation.ScrollTo();
            }));

            Parallel.For(0, Navigation.Pics.Count, i =>
            {
                GalleryLoad.UpdatePic(i, thumbs[i].pic);
            });

            thumbs = null;
        }

        internal static void Clear()
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (GetPicGallery == null)
                {
                    return;
                }

                GetPicGallery.Container.Children.Clear();

#if DEBUG
                Trace.WriteLine("Cleared Gallery children");
#endif
            }));

        }
    }
}