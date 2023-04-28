using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.UILogic;
using XamlAnimatedGif;
using static PicView.ChangeImage.LoadPic;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.UILogic.Sizing.ScaleImage;

namespace PicView.ChangeImage
{
    internal static class QuickLoad
    {
        /// <summary>
        /// Load Image from blank values and show loading preview
        /// </summary>
        /// <param name="file"></param>
        internal static async Task QuickLoadAsync(string file)
        {
            InitialPath = file;
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists) // If not file, try to load if URL, base64, archive or directory
            {
                await LoadPicFromStringAsync(file, fileInfo).ConfigureAwait(false);
                return;
            }

            LoadingPreview(fileInfo, false);
            var size = ImageSizeFunctions.GetImageSize(file);

            if (size.HasValue)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    FitImage(size.Value.Width, size.Value.Height), DispatcherPriority.Send);
            }

            var bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
            if (bitmapSource != null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    SetMainImage(bitmapSource, fileInfo), DispatcherPriority.Send);

                if (!size.HasValue)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                        FitImage(bitmapSource.Width, bitmapSource.Height), DispatcherPriority.Send);
                }
            }
            else
            {
                var errorImage = ImageFunctions.ImageErrorMessage();
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    ConfigureWindows.GetMainWindow.MainImage.Source = errorImage);
            }

            Pics = FileList(fileInfo);
            FolderIndex = Pics.IndexOf(fileInfo.FullName);

            if (bitmapSource != null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => 
                    SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, FolderIndex, fileInfo), DispatcherPriority.Send);
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    SetTitleString(0, 0, FolderIndex, fileInfo), DispatcherPriority.Send);
            }

            if (FolderIndex > 0)
            {
                Taskbar.Progress((double)FolderIndex / Pics.Count);
                await Preloader.PreLoadAsync(FolderIndex).ConfigureAwait(false);
            }

            if (bitmapSource is not null)
                await Preloader.AddAsync(FolderIndex, fileInfo, bitmapSource).ConfigureAwait(false);

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.LoadAsync().ConfigureAwait(false);
            }

            // Add recent files, except when browsing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > FolderIndex)
            {
                GetFileHistory ??= new FileHistory();
                GetFileHistory.Add(Pics[FolderIndex]);
            }            
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static void SetMainImage(ImageSource imageSource, FileInfo fileInfo)
        {
            if (fileInfo.Extension?.ToLowerInvariant() == ".gif")
            {
                AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(fileInfo.FullName));
            }
            else
            {
                ConfigureWindows.GetMainWindow.MainImage.Source = imageSource;
            }
            ConfigureWindows.GetMainWindow.MainImage.Cursor = Cursors.Arrow;
        }
    }
}