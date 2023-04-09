using PicView.FileHandling;
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
            if (!fileInfo.Exists) // If not file, try to load if URL or base64
            {
                await LoadPicFromStringAsync(file).ConfigureAwait(false);
                return;
            }
            else if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                await LoadPicFromFolderAsync(fileInfo).ConfigureAwait(false);
                return;
            }

            if (SupportedFiles.IsArchive(file))
            {
                await LoadPicFromArchiveAsync(file).ConfigureAwait(false);
                return;
            }

            LoadingPreview(fileInfo);
            var size = ImageSizeFunctions.GetImageSize(file);
            BitmapSource? bitmapSource = null;

            if (size.HasValue)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => FitImage(size.Value.Width, size.Value.Height), DispatcherPriority.Send);
            }

            bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
            if (bitmapSource != null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => SetMainImage(bitmapSource, fileInfo), DispatcherPriority.Send);
                if (!size.HasValue)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => FitImage(bitmapSource.Width, bitmapSource.Height), DispatcherPriority.Send);
                }
            }
            else
            {
                var ErrorImage = ImageFunctions.ImageErrorMessage();
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => ConfigureWindows.GetMainWindow.MainImage.Source = ErrorImage);
            }

            Pics = FileList(fileInfo);

            FolderIndex = Pics?.IndexOf(fileInfo.FullName) ?? 0;

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
            }

            await Preloader.AddAsync(FolderIndex, fileInfo, bitmapSource).ConfigureAwait(false);
            await Preloader.PreLoadAsync(FolderIndex).ConfigureAwait(false);

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.LoadAsync().ConfigureAwait(false);
            }

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > FolderIndex)
            {
                GetFileHistory ??= new FileHistory();
                GetFileHistory.Add(Pics[FolderIndex]);
            }            
        }

        static void SetMainImage(BitmapSource bitmapSource, FileInfo fileInfo)
        {
            if (fileInfo.Extension?.ToLowerInvariant() == ".gif")
            {
                AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(fileInfo.FullName));
            }
            else
            {
                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
            }
            ConfigureWindows.GetMainWindow.MainImage.Cursor = System.Windows.Input.Cursors.Arrow;
        }
    }
}