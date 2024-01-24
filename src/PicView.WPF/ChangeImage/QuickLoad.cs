using ImageMagick;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.WPF.ChangeImage.LoadPic;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.ChangeImage.PreLoader;
using static PicView.WPF.ChangeTitlebar.SetTitle;
using static PicView.WPF.FileHandling.FileLists;
using static PicView.WPF.UILogic.Sizing.ScaleImage;

namespace PicView.WPF.ChangeImage;

internal static class QuickLoad
{
    internal static async Task QuickLoadAsync(string file, FileInfo? fileInfo = null) => await Task.Run(async () =>
    {
        var mainWindow = ConfigureWindows.GetMainWindow;
        fileInfo ??= new FileInfo(file);
        if (!fileInfo.Exists) // If not file, try to load if URL, base64 or directory
        {
            await LoadPicFromStringAsync(file, fileInfo);
            return;
        }

        if (file.IsArchive()) // Handle if file exist and is an archive
        {
            await LoadPicFromArchiveAsync(file);
            return;
        }

        var bitmapSource = await Image2BitmapSource.ReturnBitmapSourceAsync(fileInfo);
        EXIFHelper.EXIFOrientation orientation = 0;
        if (bitmapSource is not null)
        {
            orientation = EXIFHelper.GetImageOrientation(new MagickImage(fileInfo));
        }

        await mainWindow.MainImage.Dispatcher.InvokeAsync(() =>
        {
            mainWindow.MainImage.Source = bitmapSource ?? ImageFunctions.ImageErrorMessage();
            if (orientation != 0)
            {
                UpdateImage.SetOrientation(orientation);
            }

            FitImage(bitmapSource?.Width ?? 0, bitmapSource?.Height ?? 0);
            UC.GetSpinWaiter.Visibility = Visibility.Collapsed;
            mainWindow.MainImage.Cursor = Cursors.Arrow;
        }, DispatcherPriority.Send);

        if (fileInfo.Extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            var frames = ImageFunctions.GetImageFrames(fileInfo.FullName);
            if (frames > 1)
            {
                var uri = new Uri(fileInfo.FullName);
                await mainWindow.Dispatcher.InvokeAsync(
                    () => { AnimationBehavior.SetSourceUri(mainWindow.MainImage, uri); },
                    DispatcherPriority.Normal);
            }
        }

        Pics = await Task.FromResult(FileList(fileInfo));
        FolderIndex = Pics.IndexOf(fileInfo.FullName);
        var shouldLoadBottomGallery = SettingsHelper.Settings.Gallery.IsBottomGalleryShown;
        if (SettingsHelper.Settings.UIProperties.ShowInterface == false)
        {
            shouldLoadBottomGallery = SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;
        }

        await mainWindow.Dispatcher.InvokeAsync(() =>
        {
            SetTitleString(bitmapSource?.PixelWidth ?? 0, bitmapSource?.PixelHeight ?? 0, FolderIndex, fileInfo);
            if (shouldLoadBottomGallery)
            {
                GalleryToggle.ShowBottomGallery();
                FitImage(bitmapSource?.Width ?? 0, bitmapSource?.Height ?? 0);
            }
        }, DispatcherPriority.Normal);

        // Add recent files, except when browsing archive
        if (string.IsNullOrWhiteSpace(ArchiveHelper.TempZipFile) && Pics.Count > FolderIndex)
        {
            FileHistoryNavigation.Add(Pics[FolderIndex]);
        }

        await AddAsync(FolderIndex, fileInfo, bitmapSource);

        if (FolderIndex > 0)
        {
            Taskbar.Progress((double)FolderIndex / Pics.Count);
            await PreLoadAsync(FolderIndex, Pics.Count, true);

            if (shouldLoadBottomGallery)
            {
                try
                {
                    await GalleryLoad.LoadAsync().ConfigureAwait(false);
                    // Update gallery selections
                    await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                    {
                        // Select current item
                        GalleryNavigation.SetSelected(FolderIndex, true);
                        GalleryNavigation.SelectedGalleryItem = FolderIndex;
                        GalleryNavigation.ScrollToGalleryCenter();
                    });
                }
                catch (Exception exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(QuickLoadAsync)} exception:\n{exception.Message}");
#endif
                    if (ConfigureWindows.GetMainWindow.Visibility == Visibility.Hidden)
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }
    });
}