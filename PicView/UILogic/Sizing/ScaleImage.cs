using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.UILogic.TransformImage;
using System;
using System.Threading.Tasks;
using static PicView.ChangeImage.Navigation;
using static PicView.PicGallery.GalleryNavigation;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.TransformImage.Rotation;
using static PicView.UILogic.TransformImage.Scroll;

namespace PicView.UILogic.Sizing
{
    internal static class ScaleImage
    {
        /// <summary>
        /// Backup of Width data
        /// </summary>
        internal static double XWidth { get; set; }

        /// <summary>
        /// Backup of Height data
        /// </summary>
        internal static double XHeight { get; set; }

        /// <summary>
        /// Used to get and set Aspect Ratio
        /// </summary>
        internal static double AspectRatio { get; set; }

        /// <summary>
        /// Tries to call Zoomfit with additional error checking
        /// </summary>
        internal static async Task<bool> TryFitImageAsync()
        {
            if (Error_Handling.CheckOutOfRange() == false)
            {
                if (Pics.Count > FolderIndex)
                {
                    var pic = ChangeImage.Preloader.Get(Pics[FolderIndex]).bitmapSource;
                    if (pic != null)
                    {
                        FitImage(pic.PixelWidth, pic.PixelHeight);
                        return true;
                    }
                    else
                    {
                        var size = await ImageFunctions.ImageSizeAsync(Pics[FolderIndex]).ConfigureAwait(false);
                        if (size.HasValue)
                        {
                            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
                            {
                                FitImage(size.Value.Width, size.Value.Height);
                            }));
                            
                            return true;
                        }
                        else if (GetMainWindow.MainImage.Source != null)
                        {
                            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() => 
                            { 
                                FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
                            }));
                            return true;
                        }
                        else if (XWidth > 0 && XHeight > 0)
                        {
                            FitImage(XWidth, XHeight);
                            return true;
                        }
                    }
                }
            }
            else if (GetMainWindow.MainImage.Source != null)
            {
                FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
                return true;
            }
            else if (XWidth > 0 && XHeight > 0)
            {
                FitImage(XWidth, XHeight);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to call Zoomfit with specified path
        /// </summary>
        internal static async Task<bool> TryFitImageAsync(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) { return false; }

            var size = await ImageFunctions.ImageSizeAsync(source).ConfigureAwait(false);
            if (size.HasValue)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
                {
                    FitImage(size.Value.Width, size.Value.Height);
                }));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Fits image size based on users screen resolution
        /// or window size
        /// </summary>
        /// <param name="width">The pixel width of the image</param>
        /// <param name="height">The pixel height of the image</param>
        internal static void FitImage(double width, double height)
        {
            if (width <= 0 || height <= 0) { return; }

            double maxWidth, maxHeight;
            var borderSpaceHeight = Properties.Settings.Default.Fullscreen ? 0 : GetMainWindow.LowerBar.Height + GetMainWindow.TitleBar.Height + 6;
            var borderSpaceWidth = Properties.Settings.Default.Fullscreen && Properties.Settings.Default.ShowAltInterfaceButtons ? 0 : 20 * MonitorInfo.DpiScaling;

            var monitorWidth = (MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - borderSpaceWidth;
            var monitorHeight = (MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - borderSpaceHeight;

            double padding;// Padding to make it feel more comfortable
            if (MonitorInfo.DpiScaling >= 1)
            {
                padding = MonitorInfo.Height - monitorHeight;
                padding = padding < 0 ? 0 : padding;
            }
            else
            {
                padding = 0;
            }

            if (Properties.Settings.Default.FullscreenGallery)
            {
                /// Extra padding for picgallery required
                padding += PicGalleryItem_Size - 50;
                maxWidth = Math.Min(monitorWidth - padding, width);
                maxHeight = Math.Min(monitorHeight, height);
            }
            else if (Properties.Settings.Default.AutoFitWindow) // If non resizeable behaviour
            {
                if (Properties.Settings.Default.FillImage) // Max to monitor height if scaling enabled, else go by min pixel width
                {
                    maxWidth = monitorWidth;
                    maxHeight = monitorHeight;
                }
                else
                {
                    /// Use padding for shown interface
                    maxWidth = Math.Min(monitorWidth - padding, width);
                    maxHeight = Math.Min(monitorHeight - padding, height);
                }
            }
            else /// Get max width and height, based on window size
            {
                if (Properties.Settings.Default.FillImage)
                {
                    maxWidth = GetMainWindow.ParentContainer.ActualWidth;
                    maxHeight = GetMainWindow.ParentContainer.ActualHeight;
                }
                else
                {
                    maxWidth = Math.Min(GetMainWindow.ParentContainer.ActualWidth, width);
                    maxHeight = Math.Min(GetMainWindow.ParentContainer.ActualHeight, height);
                }
            }

            if (Rotateint == 0 || Rotateint == 180) // Standard aspect ratio calculation
            {
                AspectRatio = Math.Min(maxWidth / width, maxHeight / height);
            }
            else // Rotated aspect ratio calculation
            {
                AspectRatio = Math.Min(maxWidth / height, maxHeight / width);
            }

            if (Properties.Settings.Default.ScrollEnabled)
            {
                /// Calculate height based on width
                GetMainWindow.MainImage.Width = maxWidth;
                GetMainWindow.MainImage.Height = maxWidth * height / width;

                /// Set mainWindow.Scroller height to aspect ratio calculation
                GetMainWindow.Scroller.Height = height * AspectRatio;

                /// Update values
                XWidth = GetMainWindow.MainImage.Width;
                XHeight = GetMainWindow.Scroller.Height;
            }
            else
            {
                /// Reset mainWindow.Scroller's height to auto
                GetMainWindow.Scroller.Height = double.NaN;

                /// Fit image by aspect ratio calculation
                /// and update values
                GetMainWindow.MainImage.Width = XWidth = width * AspectRatio;
                GetMainWindow.MainImage.Height = XHeight = height * AspectRatio;
            }

            if (GetMainWindow.WindowState == System.Windows.WindowState.Normal)
            {
                /// Update TitleBar
                var interfaceSize = 192 * MonitorInfo.DpiScaling; // logo and buttons width

                if (Properties.Settings.Default.FullscreenGallery)
                {
                    if (XWidth >= monitorWidth - (UC.GetPicGallery.ActualWidth + 5) * 1.88)
                    {
                        // Offset window to not overlap gallery
                        GetMainWindow.Left = ((MonitorInfo.WorkArea.Width - (UC.GetPicGallery.ActualWidth + 5) - (GetMainWindow.ActualWidth * MonitorInfo.DpiScaling)) / 2)
                                          + (MonitorInfo.WorkArea.Left * MonitorInfo.DpiScaling);
                        GetMainWindow.Top = ((MonitorInfo.WorkArea.Height
                                           - (GetMainWindow.Height * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Top * MonitorInfo.DpiScaling);
                    }
                    else
                    {
                        CenterWindowOnScreen();
                    }
                }
                else if (Properties.Settings.Default.AutoFitWindow)
                {
                    /// Update mainWindow.TitleBar width to dynamically fit new size
                    var x = Rotateint == 0 || Rotateint == 180 ? Math.Max(XWidth, GetMainWindow.MinWidth) : Math.Max(XHeight, GetMainWindow.MinHeight);
                    GetMainWindow.TitleText.MaxWidth = x - interfaceSize < interfaceSize ? interfaceSize : x - interfaceSize;

                    if (Properties.Settings.Default.KeepCentered)
                    {
                        CenterWindowOnScreen();
                    }
                }
                else
                {
                    /// Fix title width to window size
                    GetMainWindow.TitleText.MaxWidth = GetMainWindow.ActualWidth - interfaceSize;
                }
            }

            if (ZoomLogic.ZoomValue == 1.0)
            {
                ZoomLogic.ResetZoom(false);
            }
        }

        ///
        ///
        ///               _.._   _..---.
        ///             .-"    ;-"       \
        ///            /      /           |
        ///           |      |       _=   |
        ///           ;   _.-'\__.-')     |
        ///            `-'      |   |    ;
        ///                     |  /;   /      _,
        ///                   .-.;.-=-./-""-.-` _`
        ///                  /   |     \     \-` `,
        ///                 |    |      |     |
        ///                 |____|______|     |
        ///                  \0 / \0   /      /
        ///               .--.-""-.`--'     .'
        ///              (#   )          ,  \
        ///              ('--'          /\`  \
        ///               \       ,,  .'      \
        ///                `-._    _.'\        \
        ///       jgs          `""`    \        \
        ///
        ///       So much math!
        ///
    }
}