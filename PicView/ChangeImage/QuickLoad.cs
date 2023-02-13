using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.IO;
using System.Threading.Tasks;
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
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
                {
                    WindowSizing.SetWindowBehavior();
                });
            }

            if (!fileInfo.Exists) // If not file, try to load if URL or base64
            {
                await LoadPicFromStringAsync(file, false, fileInfo).ConfigureAwait(false);
                return;
            }
            
            await LoadingPreviewAsync(fileInfo).ConfigureAwait(false);
            var pic = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);

            if (pic is not null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
                {
                    if (fileInfo.Extension.ToLowerInvariant() == ".gif")
                    {
                        AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(fileInfo.FullName));
                    }
                    else
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Source = pic;
                    }
                    FitImage(pic.PixelWidth, pic.PixelHeight);
                });
            }

            await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

            FolderIndex = Pics.Count > 0 ? Pics.IndexOf(fileInfo.FullName) : 0;

            if (pic is not null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
                {
                    SetTitleString(pic.PixelWidth, pic.PixelHeight, FolderIndex, fileInfo);
                });
            }

            if (FolderIndex > 0)
            {
                await Taskbar.Progress((double)FolderIndex / Pics.Count).ConfigureAwait(false);
            }

            await Preloader.PreLoadAsync(FolderIndex).ConfigureAwait(false);
            await Preloader.AddAsync(FolderIndex, fileInfo, pic).ConfigureAwait(false);

            if (GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }

            FreshStartup = false;

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > FolderIndex)
            {
                if (GetFileHistory is null)
                {
                    GetFileHistory = new FileHistory();
                }

                GetFileHistory.Add(Pics?[FolderIndex]);
            }

            InitialPath = file;
        }
    }
}
