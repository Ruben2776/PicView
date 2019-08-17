using PicView.File_Logic;
using PicView.PreLoading;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.Error_Handling.Error_Handling;
using static PicView.File_Logic.ArchiveExtraction;
using static PicView.File_Logic.DeleteFiles;
using static PicView.File_Logic.FileLists;
using static PicView.Helpers.Helper;
using static PicView.Helpers.Variables;
using static PicView.Image_Logic.ImageManager;
using static PicView.Image_Logic.Resize_and_Zoom;
using static PicView.Interface_Logic.Interface;

namespace PicView.Image_Logic
{
    internal static class Navigation
    {
        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        internal static async void Pic(string path)
        {
            // Set Loading
            mainWindow.Title = mainWindow.Bar.Text = Loading;
            mainWindow.Bar.ToolTip = Loading;
            if (mainWindow.img.Source == null)
                AjaxLoadingStart();

            // Handle if from web
            if (!File.Exists(path))
            {
                if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                    PicWeb(path);
                else
                    Unload();
                return;
            }

            // If count not correct or just started, get values
            if (Pics == null)
                await GetValues(path);
            else if (Pics.Count <= FolderIndex || FolderIndex < 0 || freshStartup)
                await GetValues(path);

            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            else if (!string.IsNullOrWhiteSpace(PicPath) && Path.GetDirectoryName(path) != Path.GetDirectoryName(PicPath))
            {
                // Reset zipped values
                if (!string.IsNullOrWhiteSpace(TempZipPath))
                {
                    DeleteTempFiles();
                    TempZipPath = string.Empty;
                    RecentFiles.SetZipped(string.Empty, false);
                }

                // Reset old values and get new
                ChangeFolder();
                await GetValues(path);
            }

            // If no need to reset values, get index
            else if (Pics != null)
                FolderIndex = Pics.IndexOf(path);

            if (Pics != null)
            {
                // Fix large archive extraction error
                if (Pics.Count == 0)
                {
                    var recovery = await RecoverFailedArchiveAsync();
                    if (!recovery)
                    {
                        ToolTipStyle("Archive could not be processed");
                        Reload(true);
                        return;
                    }
                }
            }
            else
            {
                Reload(true);
                return;
            }

            if (File.Exists(Pics[FolderIndex]))
            {
                // Navigate to picture using obtained index
                Pic(FolderIndex);
            }
            else
            {
                Reload(true);
                return;
            }


            // Fix loading bug
            if (ajaxLoading.Opacity != 1 && canNavigate)
            {
                AjaxLoadingEnd();
            }

            // Load images for PicGallery if enabled
            if (Properties.Settings.Default.PicGallery > 0)
            {
                if (!picGallery.LoadComplete)
                    if (!picGallery.isLoading)
                        picGallery.Load();
            }
        }

        /// <summary>
        /// Loads image based on overloaded int.
        /// Possible out of range error if used inappropriately.
        /// </summary>
        /// <param name="x"></param>
        internal static async void Pic(int x)
        {
            // Additional error checking
            if (Pics.Count < x)
            {
                if (x == 0)
                {
                    var recovery = await RecoverFailedArchiveAsync();
                    if (!recovery)
                    {
                        ToolTipStyle("Archive could not be processed");
                        Reload(true);
                        return;
                    }
                }

                else if (!File.Exists(Pics[x]))
                    PicErrorFix(x);
                else
                    Reload(true);
                return;
            }
            if (x < 0)
            {
                var b = PicErrorFix(x);
                if (!b)
                    return;
            }


            // Add "pic" as local variable used for the image.
            // Use the Load() function load image from memory if available
            // if not, it will be null
            BitmapSource pic = Preloader.Load(Pics[x]);
            var Extension = Path.GetExtension(Pics[x]);

            if (pic == null)
            {
                mainWindow.Title = mainWindow.Bar.Text = Loading;
                mainWindow.Bar.ToolTip = Loading;

                if (!Properties.Settings.Default.ShowInterface)
                    AjaxLoadingStart();

                // Dissallow changing image while loading
                canNavigate = false;

                if (freshStartup)
                    // Load new value manually
                    await Task.Run(() => pic = RenderToBitmapSource(Pics[x], Extension));
                else do
                    {
                        // Try again while loading?
                        pic = Preloader.Load(Pics[x]);
                        await Task.Delay(25);
                    } while (Preloader.IsLoading);


                canNavigate = true;

                // If pic is still null, image can't be rendered
                if (pic == null)
                {
                    PicErrorFix(x);
                    return;
                }
            }

            // Show the image! :)
            mainWindow.img.Source = pic;

            // Fit image to new values
            ZoomFit(pic.PixelWidth, pic.PixelHeight);

            // Scroll to top if scroll enabled
            if (IsScrollEnabled)
                mainWindow.Scroller.ScrollToTop();

            //// Prevent picture from being flipped if previous is
            //if (Flipped)
            //    Flip();

            // Update values
            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, x);
            mainWindow.Title = titleString[0];
            mainWindow.Bar.Text = titleString[1];
            mainWindow.Bar.ToolTip = titleString[2];
            PicPath = Pics[x];
            FolderIndex = x;

            // Preload images \\
            if (PreloadDirection().HasValue)
            {
                await Task.Run(() =>
                {
                    Preloader.PreLoad(x, reverse);
                    PreloadCount = 0;

                    if (x < Pics.Count)
                    {
                        if (!Preloader.Contains(Pics[x]))
                            Preloader.Add(pic, Pics[x]);
                    }
                });
            }

            AjaxLoadingEnd();
            Progress(x, Pics.Count);

            if (!freshStartup)
                RecentFiles.Add(Pics[x]);

            if (picGallery != null)
            {
                if (freshStartup)
                {
                    if (!picGallery.LoadComplete)
                        return;

                    picGallery.Calculate_Paging();
                    picGallery.ScrollTo();
                }
                else
                    picGallery.ScrollTo(reverse);
            }
            freshStartup = false;
        }

        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static void Pic(BitmapSource pic, string imageName)
        {
            Unload();

            if (IsScrollEnabled)
                mainWindow.Scroller.ScrollToTop();

            mainWindow.img.Source = pic;

            ZoomFit(pic.PixelWidth, pic.PixelHeight);
            CloseToolTipStyle();

            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, imageName);
            mainWindow.Title = titleString[0];
            mainWindow.Bar.Text = titleString[1];
            mainWindow.Bar.ToolTip = titleString[1];

            NoProgress();
            PicPath = string.Empty;

            canNavigate = false;
        }

        /// <summary>
        /// Goes to next, previous, first or last file in folder
        /// </summary>
        /// <param name="next">Whether it's forward or not</param>
        /// <param name="end">Whether to go to last or first,
        /// depending on the next value</param>
        internal static void Pic(bool next = true, bool end = false)
        {
            // Exit if not intended to change picture
            if (!canNavigate)
                return;

            // .. Or browsing PicGallery
            if (picGallery != null)
            {
                if (Properties.Settings.Default.PicGallery == 1)
                    if (picGallery.open)
                        return;
            }

            // Go to first or last
            if (end)
            {
                FolderIndex = next ? Pics.Count - 1 : 0;

                if (!Preloader.Contains(Pics[FolderIndex]))
                {
                    PreloadCount = 0;
                    Preloader.Clear();
                }
                else
                {
                    if (next)
                        PreloadCount++;
                    else
                        PreloadCount--;
                }
            }
            // Go to next or previous
            else
            {
                if (next)
                {
                    // loop next
                    if (Properties.Settings.Default.Looping)
                    {
                        FolderIndex = FolderIndex == Pics.Count - 1 ? 0 : FolderIndex + 1;
                    }
                    else
                    {
                        // Go to next if able
                        if (FolderIndex + 1 == Pics.Count)
                            return;
                        FolderIndex++;
                    }

                    PreloadCount++;
                    reverse = false;
                }
                else
                {
                    // Loop prev
                    if (Properties.Settings.Default.Looping)
                    {
                        FolderIndex = FolderIndex == 0 ? Pics.Count - 1 : FolderIndex - 1;
                    }
                    else
                    {
                        // Go to prev if able
                        if (FolderIndex - 1 < 0)
                            return;
                        FolderIndex--;
                    }

                    PreloadCount--;
                    reverse = true;
                }
            }
            Pic(FolderIndex);
        }

        /// <summary>
        /// Only load image from preload or thumbnail without resizing
        /// </summary>
        internal static async void FastPic(object sender, EventArgs e)
        {
            await mainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                mainWindow.Bar.ToolTip =
                mainWindow.Title =
                mainWindow.Bar.Text = "Image " + (FolderIndex + 1) + " of " + Pics.Count;

                mainWindow.img.Width = xWidth;
                mainWindow.img.Height = xHeight;

                mainWindow.img.Source = Preloader.Contains(Pics[FolderIndex]) ? Preloader.Load(Pics[FolderIndex]) : GetBitmapSourceThumb(Pics[FolderIndex]);
            }));
            Progress(FolderIndex, Pics.Count);
            FastPicRunning = true;
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static void FastPicUpdate()
        {
            fastPicTimer.Stop();
            FastPicRunning = false;

            //if (!Preloader.Contains(Pics[FolderIndex]))
            //{
            //    PreloadCount = 0;
            //    Preloader.Clear();
            //}

            Pic(FolderIndex);
        }

        /// <summary>
        /// Attemps to download image and display it
        /// </summary>
        /// <param name="path"></param>
        internal static async void PicWeb(string path)
        {
            if (ajaxLoading.Opacity != 1)
                AjaxLoadingStart();

            mainWindow.Bar.Text = Loading;

            BitmapSource pic;
            try
            {
                pic = await LoadImageWebAsync(path);
            }
            catch (Exception)
            {
                pic = null;
            }

            if (pic == null)
            {
                Reload(true);
                ToolTipStyle("Unable to load image");
                AjaxLoadingEnd();
                return;
            }

            Pic(pic, path);
            PicPath = path;
            RecentFiles.Add(path);
        }

        /// <summary>
        /// Downloads image from web and returns as BitmapSource
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal static async Task<BitmapSource> LoadImageWebAsync(string address)
        {
            BitmapSource pic = null;
            await Task.Run(async () =>
            {
                var client = new WebClient();
                client.DownloadProgressChanged += (sender, e) =>
                    Application.Current.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        mainWindow.Title = mainWindow.Bar.Text = e.BytesReceived + "/" + e.TotalBytesToReceive + ". " + e.ProgressPercentage + "% complete...";
                    }));

                var bytes = await client.DownloadDataTaskAsync(new Uri(address));
                var stream = new MemoryStream(bytes);
                pic = GetMagickImage(stream);
            });
            return pic;
        }
    }
}
