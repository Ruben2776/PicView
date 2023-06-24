using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.ChangeImage.LoadPic;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.UILogic.Sizing.ScaleImage;

namespace PicView.ChangeImage;

internal static class QuickLoad
{
    /// <summary>
    /// Load Image from blank values and show loading preview
    /// </summary>
    /// <param name="file"></param>
    internal static async Task QuickLoadAsync(string file)
    {
        var mainWindow = ConfigureWindows.GetMainWindow;
        InitialPath = file;
        var fileInfo = new FileInfo(file);
        if (!fileInfo.Exists) // If not file, try to load if URL, base64 or directory
        {
            await LoadPicFromStringAsync(file, fileInfo).ConfigureAwait(false);
            return;
        }
        if (file.IsArchive()) // Handle if file exist and is archive
        {
            await LoadPicFromArchiveAsync(file).ConfigureAwait(false);
            return;
        }

        LoadingPreview(fileInfo, false);
        var size = ImageSizeFunctions.GetImageSize(file);
        if (Settings.Default.FullscreenGallery)
        {
            GalleryNavigation.SetSize(Settings.Default.BottomGalleryItems);
        }

        if (size.HasValue)
        {
            await mainWindow.Dispatcher.InvokeAsync(() =>
                FitImage(size.Value.Width, size.Value.Height), DispatcherPriority.Send);
        }

        var bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
        if (bitmapSource != null)
        {
            await mainWindow.Dispatcher.InvokeAsync(() =>
                SetMainImage(bitmapSource, fileInfo), DispatcherPriority.Send);

            if (!size.HasValue)
            {
                await mainWindow.Dispatcher.InvokeAsync(() =>
                    FitImage(bitmapSource.Width, bitmapSource.Height), DispatcherPriority.Send);
            }
        }
        else
        {
            var errorImage = ImageFunctions.ImageErrorMessage();
            await mainWindow.Dispatcher.InvokeAsync(() => mainWindow.MainImage.Source = errorImage);
        }

        await mainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (UC.GetSpinWaiter is { IsVisible: true })
            {
                UC.GetSpinWaiter.Visibility = Visibility.Collapsed;
            }
        });

        Pics = FileList(fileInfo);
        FolderIndex = Pics.IndexOf(fileInfo.FullName);

        if (bitmapSource != null)
        {
            await mainWindow.Dispatcher.InvokeAsync(() =>
                SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, FolderIndex, fileInfo), DispatcherPriority.Send);
        }
        else
        {
            await mainWindow.Dispatcher.InvokeAsync(() =>
                SetTitleString(0, 0, FolderIndex, fileInfo), DispatcherPriority.Send);
        }

        if (FolderIndex > 0)
        {
            Taskbar.Progress((double)FolderIndex / Pics.Count);
            await PreLoader.PreLoadAsync(FolderIndex).ConfigureAwait(false);
        }

        if (bitmapSource is not null)
            await PreLoader.AddAsync(FolderIndex, fileInfo, bitmapSource).ConfigureAwait(false);

        if (Settings.Default.FullscreenGallery || Settings.Default.IsBottomGalleryShown)
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