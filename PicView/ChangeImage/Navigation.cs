using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.SystemIntegration;
using PicView.UI.PicGallery;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Error_Handling;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.ImageHandling.Thumbnails;
using static PicView.Library.Fields;
using static PicView.UI.SetTitle;
using static PicView.UI.Sizing.ScaleImage;
using static PicView.UI.Tooltip;
using static PicView.UI.TransformImage.Scroll;
using static PicView.UI.UserControls.UC;

namespace PicView.ChangeImage
{
    internal static class Navigation
    {
        #region Update Image values

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        internal static async void Pic(string path)
        {
            // Set Loading
            mainWindow.Title = mainWindow.Bar.Text = Loading;
            mainWindow.Bar.ToolTip = Loading;

            // Handle if from web
            if (!File.Exists(path))
            {
                if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                {
                    LoadFromWeb.PicWeb(path);
                    return;
                }
                else if (Directory.Exists(path))
                {
                    ChangeFolder(true);
                    GetValues(path);
                }
                else
                {
                    Unload();
                    return;
                }
            }

            // If count not correct or just started, get values
            if (Pics.Count <= FolderIndex || FolderIndex < 0 || FreshStartup)
            {
                GetValues(path);
            }
            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            else if (!string.IsNullOrWhiteSpace(Pics[FolderIndex]) && Path.GetDirectoryName(path) != Path.GetDirectoryName(Pics[FolderIndex]))
            {
                // Reset old values and get new
                ChangeFolder(true);
                GetValues(path);
            }

            // If no need to reset values, get index
            else if (Pics != null)
            {
                FolderIndex = Pics.IndexOf(path);
            }

            if (Pics != null)
            {
                // Fix large archive extraction error
                if (Pics.Count == 0)
                {
                    var recovery = await RecoverFailedArchiveAsync().ConfigureAwait(true);
                    if (!recovery)
                    {
                        ShowTooltipMessage("Archive could not be processed");
                        Reload(true);
                        return;
                    }
                    else
                    {
                        mainWindow.Bar.Text = "Unzipping...";
                        mainWindow.Bar.ToolTip = mainWindow.Bar.Text;
                    }
                    mainWindow.Focus();
                }
            }
            else
            {
                Reload(true);
                return;
            }

            if (!FreshStartup)
            {
                Preloader.Clear();
            }

#if DEBUG
            if (FreshStartup)
                Trace.WriteLine("Pic(string path) entering Pic(int x)");
#endif

            // Navigate to picture using obtained index
            Pic(FolderIndex);

            prevPicResource = null; // Make sure to not waste memory
        }

        /// <summary>
        /// Loads image based on overloaded int.
        /// </summary>
        /// <param name="x">The index of file to load from Pics</param>
        internal static async void Pic(int x)
        {
#if DEBUG
            var stopWatch = new Stopwatch();
            stopWatch.Start();
#endif

            BitmapSource pic;

            if (Pics.Count <= x)
            {
                pic = await PicErrorFix(x).ConfigureAwait(true);
                if (pic == null)
                {
                    Reload(true);
                    return;
                }
            }
            else
            {
                /// Add "pic" as local variable used for the image.
                /// Use the Load() function load image from memory if available
                /// if not, it will be null
                pic = Preloader.Load(Pics[x]);
            }

            if (pic == null)
            {
                mainWindow.Title = Loading;
                mainWindow.Bar.Text = Loading;
                mainWindow.Bar.ToolTip = Loading;

                var thumb = GetThumb(x, true);

                if (thumb != null)
                {
                    mainWindow.img.Source = thumb;
                }

                // Dissallow changing image while loading
                CanNavigate = false;

                if (FreshStartup || Preloader.GetIsReset())
                {
                    await Preloader.Add(Pics[x]).ConfigureAwait(false);
                    Preloader.SetIsReset(false);
#if DEBUG
                    Trace.WriteLine("Pic(int x) loading new pic manually");
#endif
                }

                do
                {
                    // Try again while loading
                    pic = Preloader.Load(Pics[x]);
                    await Task.Delay(3).ConfigureAwait(false);
                } while (Preloader.GetIsLoading());

                // If pic is still null, image can't be rendered
                if (pic == null)
                {
                    // Attempt to load new image
                    pic = await PicErrorFix(x).ConfigureAwait(true);
                    if (pic == null)
                    {
                        if (Pics.Count <= 1)
                        {
                            Unload();
                            return;
                        }

                        pic = DisplayUnsupportedImage.DrawUnsupportedImageText();
                        CanNavigate = true;
                    }
                }
            }

            await mainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, (Action)(() =>
            {
                // Show the image! :)
                mainWindow.img.Source = pic;
                FitImage(pic.PixelWidth, pic.PixelHeight);
                SetTitleString(pic.PixelWidth, pic.PixelHeight, x);

                // Scroll to top if scroll enabled
                if (IsScrollEnabled)
                {
                    mainWindow.Scroller.ScrollToTop();
                }

            }));            

            // Update values
            CanNavigate = true;
            FolderIndex = x;

            if (Pics.Count > 1)
            {
                Taskbar.Progress(x, Pics.Count);

                // Preload images \\
                if (Preloader.StartPreload())
                {
                    await Preloader.PreLoad(x).ConfigureAwait(false);
                }
            }

            if (!FreshStartup)
            {
                RecentFiles.Add(Pics[x]);

                if (prevPicResource != null)
                {
                    prevPicResource = null;
                }
            }

            FreshStartup = false;
#if DEBUG
            stopWatch.Stop();
            var s = $"Pic(); executed in {stopWatch.Elapsed.TotalMilliseconds} milliseconds";
            Trace.WriteLine(s);
#endif
        }

        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static void Pic(BitmapSource bitmap, string imageName)
        {
            /// Old method, might need updates?

            Unload();

            if (IsScrollEnabled)
            {
                mainWindow.Scroller.ScrollToTop();
            }

            mainWindow.img.Source = bitmap;

            FitImage(bitmap.PixelWidth, bitmap.PixelHeight);
            CloseToolTipMessage();

            SetTitleString(bitmap.PixelWidth, bitmap.PixelHeight, imageName);

            Taskbar.NoProgress();

            CanNavigate = false;
        }

        /// <summary>
        /// Load a picture from a base64
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static async void Pic64(string base64string)
        {
            var pic = await Base64.Base64StringToBitmap(base64string).ConfigureAwait(true);

            Unload();

            if (IsScrollEnabled)
            {
                mainWindow.Scroller.ScrollToTop();
            }

            mainWindow.img.Source = pic;

            FitImage(pic.PixelWidth, pic.PixelHeight);
            CloseToolTipMessage();

            SetTitleString(pic.PixelWidth, pic.PixelHeight, "Base64 image");

            Taskbar.NoProgress();

            CanNavigate = false;
        }

        internal static void PicFolder(string folder)
        {
            ChangeFolder(true);
            Pics = FileList(folder);

            Pic(0);

            quickSettingsMenu.GoToPicBox.Text = (FolderIndex + 1).ToString(CultureInfo.CurrentCulture);

            prevPicResource = null; // Make sure to not waste memory
        }

        #endregion Update Image values

        #region Change navigation values

        /// <summary>
        /// Goes to next, previous, first or last file in folder
        /// </summary>
        /// <param name="next">Whether it's forward or not</param>
        /// <param name="end">Whether to go to last or first,
        /// depending on the next value</param>
        internal static void Pic(bool next = true, bool end = false)
        {
            // Exit if not intended to change picture
            if (!CanNavigate)
            {
                return;
            }

            // exit if browsing PicGallery
            if (picGallery != null)
            {
                if (Properties.Settings.Default.PicGallery == 1)
                {
                    if (GalleryFunctions.IsOpen)
                    {
                        return;
                    }
                }
            }

            // Make backup
            var x = FolderIndex;

            // Go to first or last
            if (end)
            {
                FolderIndex = next ? Pics.Count - 1 : 0;
                x = FolderIndex;

                // Reset preloader values to prevent errors
                if (Pics.Count > 20)
                {
                    Preloader.Clear();
                }

                PreloadCount = 4;
            }
            // Go to next or previous
            else
            {
                if (next)
                {
                    // loop next
                    if (Properties.Settings.Default.Looping || SlideTimer != null && SlideTimer.Enabled)
                    {
                        FolderIndex = FolderIndex == Pics.Count - 1 ? 0 : FolderIndex + 1;
                    }
                    else
                    {
                        // Go to next if able
                        if (FolderIndex + 1 == Pics.Count)
                        {
                            return;
                        }

                        FolderIndex++;
                    }

                    PreloadCount++;
                    Reverse = false;
                }
                else
                {
                    // Loop prev
                    if (Properties.Settings.Default.Looping || SlideTimer != null && SlideTimer.Enabled)
                    {
                        FolderIndex = FolderIndex == 0 ? Pics.Count - 1 : FolderIndex - 1;
                    }
                    else
                    {
                        // Go to prev if able
                        if (FolderIndex - 1 < 0)
                        {
                            return;
                        }

                        FolderIndex--;
                    }

                    PreloadCount--;
                    Reverse = true;
                }
            }

            // Go to the image!
            Pic(FolderIndex);

            // Update PicGallery selected item, if needed
            if (picGallery != null)
            {
                if (picGallery.Container.Children.Count > FolderIndex && picGallery.Container.Children.Count > x)
                {
                    if (x != FolderIndex)
                    {
                        GalleryFunctions.SetUnselected(x);
                    }

                    GalleryFunctions.SetSelected(FolderIndex);
                    GalleryScroll.ScrollTo();
                }
                else
                {
                    // TODO Find way to get PicGalleryItem an alternative way...
                }
            }
        }

        internal static void PicButton(bool arrow, bool right)
        {
            if (arrow)
            {
                if (!CanNavigate)
                {
                    return;
                }

                if (right)
                {
                    ClickArrowRightClicked = true;
                    Pic();
                }
                else
                {
                    ClickArrowLeftClicked = true;
                    Pic(false, false);
                }
            }
            else
            {
                if (GalleryFunctions.IsOpen)
                {
                    GalleryScroll.ScrollTo(!right);
                    return;
                }

                if (!CanNavigate)
                {
                    return;
                }

                if (right)
                {
                    RightbuttonClicked = true;
                    Pic();
                }
                else
                {
                    LeftbuttonClicked = true;
                    Pic(false, false);
                }
            }
        }

        /// <summary>
        /// Only load image without resizing
        /// </summary>
        /// <param name="forwards">The direction</param>
        internal static void FastPic(bool forwards)
        {
            /// TODO FastPic Changes...
            /// Need solution for slowing down this thing to something useful
            /// await task delay only works once, it seems
            /// Timers doesn't deliver a proper result in my experience
            ///

            FastPicRunning = true;

            if (forwards)
            {
                if (FolderIndex == Pics.Count - 1)
                {
                    FolderIndex = 0;
                }
                else
                {
                    FolderIndex++;
                }
            }
            else
            {
                if (FolderIndex == 0)
                {
                    FolderIndex = Pics.Count - 1;
                }
                else
                {
                    FolderIndex--;
                }
            }

            mainWindow.img.Width = xWidth;
            mainWindow.img.Height = xHeight;

            mainWindow.Bar.ToolTip =
            mainWindow.Title =
            mainWindow.Bar.Text = "Image " + (FolderIndex + 1) + " of " + Pics.Count;

            var thumb = GetThumb(FolderIndex);

            if (thumb != null)
            {
                mainWindow.img.Source = thumb;
            }

            Taskbar.Progress(FolderIndex, Pics.Count);
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static void FastPicUpdate()
        {
            if (!FastPicRunning)
            {
                return;
            }

            FastPicRunning = false;

            if (!Preloader.Contains(Pics[FolderIndex]))
            {
                PreloadCount = 0;
                Preloader.Clear();
            }

            Pic(FolderIndex);
        }

        #endregion Change navigation values
    }
}