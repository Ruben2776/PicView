using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.WPF.ChangeImage.LoadPic;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.ChangeImage.PreLoader;
using static PicView.WPF.ChangeTitlebar.SetTitle;
using static PicView.WPF.FileHandling.FileLists;
using static PicView.WPF.UILogic.Sizing.ScaleImage;

namespace PicView.WPF.ChangeImage
{
    internal static class QuickLoad
    {
        /// <summary>
        /// Load Image from blank values and show loading preview
        /// </summary>
        /// <param name="file"></param>
        internal static async Task QuickLoadAsync(string file, FileInfo? fileInfo = null) => await Task.Run(async () =>
        {
            var mainWindow = ConfigureWindows.GetMainWindow;
            fileInfo ??= new FileInfo(file);
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

            var bitmapSource = await Image2BitmapSource.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
            await mainWindow.MainImage.Dispatcher.InvokeAsync(() =>
            {
                mainWindow.MainImage.Source = bitmapSource;
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

            Pics = await Task.FromResult(FileList(fileInfo)).ConfigureAwait(false);
            FolderIndex = Pics.IndexOf(fileInfo.FullName);
            var shouldLoadBottomGallery = SettingsHelper.Settings.Gallery.IsBottomGalleryShown;
            if (SettingsHelper.Settings.UIProperties.ShowInterface == false)
            {
                shouldLoadBottomGallery = SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;
            }

            await mainWindow.Dispatcher.InvokeAsync(() =>
            {
                SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, FolderIndex, fileInfo);
                UC.GetSpinWaiter.Visibility = Visibility.Collapsed;
                mainWindow.MainImage.Cursor = Cursors.Arrow;
                if (shouldLoadBottomGallery)
                {
                    GalleryToggle.ShowBottomGallery();
                }

                FitImage(bitmapSource.Width, bitmapSource.Height);
            }, DispatcherPriority.Normal);

            if (FolderIndex > 0)
            {
                Taskbar.Progress((double)FolderIndex / Pics.Count);
                _ = PreLoadAsync(FolderIndex, Pics.Count).ConfigureAwait(false);
            }

            _ = AddAsync(FolderIndex, fileInfo, bitmapSource).ConfigureAwait(false);

            if (shouldLoadBottomGallery)
            {
                _ = Task.Run(async () =>
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
                    catch (TaskCanceledException exception)
                    {
#if DEBUG
                        Trace.WriteLine($"{nameof(QuickLoadAsync)}  exception:\n{exception.Message}");
#endif
                        if (ConfigureWindows.GetMainWindow.Visibility == Visibility.Hidden)
                        {
                            Environment.Exit(0);
                        }
                    }
                    catch (Exception)
                    {
                        //
                    }
                });
            }

            // Add recent files, except when browsing archive
            if (string.IsNullOrWhiteSpace(ArchiveExtraction.TempZipFile) && Pics.Count > FolderIndex)
            {
                FileHistoryNavigation.Add(Pics[FolderIndex]);
            }
        });
    }
}