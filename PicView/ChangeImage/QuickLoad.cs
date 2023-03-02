using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.ChangeImage.LoadPic;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.UILogic.Sizing.ScaleImage;

namespace PicView.ChangeImage
{
    internal class QuickLoad
    {

        /// <summary>
        /// Quickly load image and then update values
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static async Task QuickLoadAsync(string file)
        {
            var fileInfo = new FileInfo(file);

            if (!GalleryFunctions.IsHorizontalFullscreenOpen) // Fix window sizing
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, WindowSizing.SetWindowBehavior);
            }

            if (!fileInfo.Exists) // If not file, try to load if URL or base64
            {
                await LoadPicFromStringAsync(file).ConfigureAwait(false);
                return;
            }

            LoadingPreview(fileInfo);
            var size = await ImageSizeFunctions.GetImageSizeAsync(fileInfo).ConfigureAwait(false);
            BitmapSource? bitmapSource = null;

            if (size.HasValue)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
                FitImage(size.Value.Width, size.Value.Height));
            }

            bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
            if (bitmapSource != null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () => SetMainImage(bitmapSource, fileInfo));         
            }

            await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

            FolderIndex = Pics?.IndexOf(fileInfo.FullName) ?? 0;

            if (bitmapSource != null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
                    SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, FolderIndex, fileInfo));
            }

            if (FolderIndex > 0)
            {
                Taskbar.Progress((double)FolderIndex / Pics.Count);
            }

            await Preloader.AddAsync(FolderIndex, fileInfo, bitmapSource).ConfigureAwait(false);
            await Preloader.PreLoadAsync(FolderIndex).ConfigureAwait(false);      

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > FolderIndex)
            {
                GetFileHistory ??= new FileHistory();
                GetFileHistory.Add(Pics[FolderIndex]);
            }

            FreshStartup = false;
            InitialPath = file;
        }

        private static void SetMainImage(BitmapSource bitmapSource, FileInfo fileInfo)
        {
            if (fileInfo.Extension?.ToLowerInvariant() == ".gif")
            {
                AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(fileInfo.FullName));
            }
            else
            {
                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
            }
        }
    }
}
