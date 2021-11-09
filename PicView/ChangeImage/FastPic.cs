using PicView.UILogic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;

namespace PicView.ChangeImage
{
    internal class FastPic
    {
        static System.Timers.Timer? timer;

        internal static void Stop() { timer = null; }

        internal static async Task Run(int index)
        {
            if (timer is null)
            {
                timer = new System.Timers.Timer(450)
                {
                    AutoReset = false,
                    Enabled = true
                };
            }
            else if (timer.Enabled)
            {
                return;
            }

            timer.Start();

            var image = Application.Current.Resources["Image"] as string; // Show string 'image' fron translation service
            var thumb = ImageHandling.Thumbnails.GetBitmapSourceThumb(new FileInfo(Pics[index])); // Load thumb

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                ConfigureWindows.GetMainWindow.TitleText.ToolTip =
                ConfigureWindows.GetMainWindow.Title =
                ConfigureWindows.GetMainWindow.TitleText.Text
                = $"{image} {index + 1} / {Pics?.Count}";

                if (thumb != null)
                {
                    ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
                }
            });

            FolderIndex = index;
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static async Task FastPicUpdateAsync()
        {
            timer = null;

            Preloader.PreloadValue? preloadValue;

            // Reset preloader values to prevent errors
            if (Pics?.Count > 10)
            {
                Preloader.Clear();
                await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
                preloadValue = Preloader.Get(Navigation.Pics[FolderIndex]);
            }
            else
            {
                preloadValue = Preloader.Get(Navigation.Pics[FolderIndex]);

                if (preloadValue == null) // Error correctiom
                {
                    await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
                    preloadValue = Preloader.Get(Navigation.Pics[FolderIndex]);
                }
                while (preloadValue != null && preloadValue.isLoading)
                {
                    // Wait for finnished result
                    await Task.Delay(5).ConfigureAwait(false);
                }
            }

            if (preloadValue == null || preloadValue.bitmapSource == null)
            {
                await Error_Handling.ReloadAsync().ConfigureAwait(false);
                return;
            }

            LoadPic.UpdatePic(FolderIndex, preloadValue.bitmapSource);
        }
    }
}
