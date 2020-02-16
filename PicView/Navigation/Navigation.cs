using PicView.PreLoading;
using PicView.UserControls;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static PicView.AjaxLoader;
using static PicView.ArchiveExtraction;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.FileLists;
using static PicView.Helper;
using static PicView.ImageDecoder;
using static PicView.Resize_and_Zoom;
using static PicView.Scroll;
using static PicView.SetTitle;
using static PicView.Thumbnails;
using static PicView.Tooltip;

namespace PicView
{
    internal static class Navigation
    {
        #region Update Pic
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
                    LoadFromWeb.PicWeb(path);
                else
                    Unload();
                return;
            }

            // If count not correct or just started, get values
            if (Pics.Count <= FolderIndex || FolderIndex < 0 || freshStartup)
            {
                await GetValues(path).ConfigureAwait(true);
            }
            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            else if (!string.IsNullOrWhiteSpace(Pics[FolderIndex]) && Path.GetDirectoryName(path) != Path.GetDirectoryName(Pics[FolderIndex]))
            {
                //// Reset zipped values
                //if (!string.IsNullOrWhiteSpace(TempZipPath))
                //{
                //    DeleteTempFiles();
                //    TempZipPath = string.Empty;
                //    RecentFiles.SetZipped(string.Empty, false);
                //}

                // Reset old values and get new
                ChangeFolder(true);
                await GetValues(path).ConfigureAwait(true);
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
                        if (sexyToolTip.Opacity == 0)
                            ToolTipStyle("Archive could not be processed");

                        Reload(true);
                        return;
                    }
                    mainWindow.Focus();
                }
            }
            else
            {
                Reload(true);
                return;
            }

            if (File.Exists(Pics[FolderIndex]))
            {
                if (!freshStartup)
                    Preloader.Clear();

                // Navigate to picture using obtained index
                Pic(FolderIndex);
            }
            else
            {
                Reload(true);
                return;
            }           

            // Load images for PicGallery if enabled
            if (Properties.Settings.Default.PicGallery > 0)
            {
                if (!PicGalleryLogic.IsLoading)
                    await PicGalleryLoad.Load().ConfigureAwait(true);
            }

            prevPicResource = null; // Make sure to not waste memory
        }

        /// <summary>
        /// Loads image based on overloaded int.
        /// </summary>
        /// <param name="x">The index of file to load from Pics</param>
        internal static async void Pic(int x)
        {
            BitmapSource pic;

            // Additional error checking
            if (Pics.Count <= x)
            {
                if (x == 0)
                {
                    var recovery = await RecoverFailedArchiveAsync().ConfigureAwait(true);
                    if (!recovery)
                    {
                        ToolTipStyle("Archive could not be processed");
                        Reload(true);
                        return;
                    }
                }

                // Untested code
                pic = await PicErrorFix(x).ConfigureAwait(true);
                if (pic == null)
                {
                    Reload(true);
                    return;
                }
            }
            if (x < 0)
            {
                pic = await PicErrorFix(x).ConfigureAwait(true);
            }
            //if (!canNavigate)
            //{
            //    Reload(true);
            //    return;
            //}
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

                TryZoomFit(Pics[x]);

                var thumb = GetThumb();

                if (thumb != null)
                    mainWindow.img.Source = thumb;

                // Dissallow changing image while loading
                canNavigate = false;

                if (freshStartup)
                    // Load new value manually
                    await Task.Run(() => pic = RenderToBitmapSource(Pics[x])).ConfigureAwait(true);
                else
                {
                    do
                    {
                        // Try again while loading?                      
                        await Task.Delay(20).ConfigureAwait(true);
                        if (x < Pics.Count)
                        {
                            pic = Preloader.Load(Pics[x]);
                        }
                    } while (Preloader.IsLoading);
                }
                
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

                        DisplayBrokenImage();
                        canNavigate = true;
                        return;
                    }
                }
            }

            // Clear unsupported image window, if shown
            if (mainWindow.img.Source == null && !freshStartup)
            {
                if (mainWindow.topLayer.Children.Count > 0)
                {
                    mainWindow.topLayer.Children.Clear();
                }
            }

            // Show the image! :)
            mainWindow.img.Source = pic;

            ZoomFit(pic.PixelWidth, pic.PixelHeight);

            // Scroll to top if scroll enabled
            if (IsScrollEnabled)
                mainWindow.Scroller.ScrollToTop();

            /// TODO Make it staying flipped a user preference 
            //// Prevent picture from being flipped if previous is
            //if (Flipped)
            //    Flip();

            // Update values
            canNavigate = true;
            SetTitleString(pic.PixelWidth, pic.PixelHeight, x);
            FolderIndex = x;
            AjaxLoadingEnd();

            if (Pics.Count > 0)
            {
                Progress(x, Pics.Count);

                // Preload images \\
                if (Preloader.StartPreload())
                {
                    Preloader.Add(pic, Pics[FolderIndex]);
                    await Preloader.PreLoad(x).ConfigureAwait(false);

                    // Update if changed file list size
                    if (PreloadCount == 4 && FolderIndex == x)
                        SetTitleString(pic.PixelWidth, pic.PixelHeight, x);
                }
            }

            if (!freshStartup)
                RecentFiles.Add(Pics[x]);
            
            freshStartup = false;
        }

        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static void Pic(BitmapSource pic, string imageName)
        {
            /// Old method, might need updates?

            Unload();

            if (IsScrollEnabled)
                mainWindow.Scroller.ScrollToTop();

            mainWindow.img.Source = pic;

            ZoomFit(pic.PixelWidth, pic.PixelHeight);
            CloseToolTipStyle();

            SetTitleString(pic.PixelWidth, pic.PixelHeight, imageName);

            NoProgress();

            canNavigate = false;
        }

        #endregion

        #region Change Image

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

            // exit if browsing PicGallery
            if (picGallery != null)
            {
                if (Properties.Settings.Default.PicGallery == 1)
                    if (PicGalleryLogic.IsOpen)
                        return;
            }

            // Make backup?
            var x = FolderIndex;

            // Go to first or last
            if (end)
            {
                FolderIndex = next ? Pics.Count - 1 : 0;

                // Reset preloader values to prevent errors
                if (Pics.Count > 20)
                    Preloader.Clear();

                PreloadCount = 4;
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

            // Go to the image!
            Pic(FolderIndex);

            // Update PicGallery selected item, if needed
            if (picGallery != null)
            {
                if (Properties.Settings.Default.PicGallery > 0)
                {
                    if (picGallery.Container.Children.Count > FolderIndex)
                    {
                        var prevItem = picGallery.Container.Children[x] as PicGalleryItem;
                        prevItem.SetSelected(false);

                        var nextItem = picGallery.Container.Children[FolderIndex] as PicGalleryItem;
                        nextItem.SetSelected(true);
                        PicGalleryScroll.ScrollTo();
                    }
                    else
                    {
                        // TODO Find way to get PicGalleryItem an alternative way...
                    }
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
                    FolderIndex = 0;
                else
                    FolderIndex++;
            }
            else
            {
                if (FolderIndex == 0)
                    FolderIndex = Pics.Count - 1;
                else
                    FolderIndex--;
            }

            mainWindow.img.Width = xWidth;
            mainWindow.img.Height = xHeight;
            
            mainWindow.Bar.ToolTip =
            mainWindow.Title =
            mainWindow.Bar.Text = "Image " + (FolderIndex + 1) + " of " + Pics.Count;

            var thumb = GetThumb();

            if (thumb != null)
                mainWindow.img.Source = thumb;

            Progress(FolderIndex, Pics.Count);
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static void FastPicUpdate()
        {
            if (!FastPicRunning)
                return;

            //fastPicTimer.Stop();
            FastPicRunning = false;

            if (!Preloader.Contains(Pics[FolderIndex]))
            {
                PreloadCount = 0;
                Preloader.Clear();
            }

            Pic(FolderIndex);
        }

        #endregion
    }
}
