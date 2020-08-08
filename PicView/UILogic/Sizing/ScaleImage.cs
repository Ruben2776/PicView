using PicView.ImageHandling;
using PicView.UILogic.Loading;
using PicView.UILogic.TransformImage;
using System;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.PicGallery.GalleryFunctions;
using static PicView.UILogic.Sizing.WindowLogic;
using static PicView.UILogic.TransformImage.Rotation;
using static PicView.UILogic.TransformImage.Scroll;

namespace PicView.UILogic.Sizing
{
    internal static class ScaleImage
    {
        /// <summary>
        /// Backup of Width data
        /// </summary>
        internal static double xWidth;

        /// <summary>
        /// Backup of Height data
        /// </summary>
        internal static double xHeight;

        /// <summary>
        /// Used to get and set Aspect Ratio
        /// </summary>
        internal static double AspectRatio { get; set; }

        /// <summary>
        /// Tries to call Zoomfit with additional error checking
        /// </summary>
        internal static bool TryFitImage()
        {
            if (FreshStartup) { return false; }

            if (Pics != null)
            {
                if (Pics.Count > FolderIndex)
                {
                    var pic = ChangeImage.Preloader.Get(FolderIndex);
                    if (pic != null)
                    {
                        FitImage(pic.PixelWidth, pic.PixelHeight);
                        return true;
                    }
                    else
                    {
                        var size = ImageDecoder.ImageSize(Pics[FolderIndex]);
                        if (size.HasValue)
                        {
                            FitImage(size.Value.Width, size.Value.Height);
                            return true;
                        }
                        else if (LoadWindows.GetMainWindow.MainImage.Source != null)
                        {
                            FitImage(LoadWindows.GetMainWindow.MainImage.Source.Width, LoadWindows.GetMainWindow.MainImage.Source.Height);
                            return true;
                        }
                        else if (xWidth != 0 && xHeight != 0)
                        {
                            FitImage(xWidth, xHeight);
                            return true;
                        }
                    }
                }
            }
            else if (LoadWindows.GetMainWindow.MainImage.Source != null)
            {
                FitImage(LoadWindows.GetMainWindow.MainImage.Source.Width, LoadWindows.GetMainWindow.MainImage.Source.Height);
                return true;
            }
            else if (xWidth != 0 && xHeight != 0)
            {
                FitImage(xWidth, xHeight);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to call Zoomfit with specified path
        /// </summary>
        internal static bool TryFitImage(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) { return false; }

            var size = ImageDecoder.ImageSize(source);
            if (size.HasValue)
            {
                FitImage(size.Value.Width, size.Value.Height);
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
            // TODO improve sizing for high DPI cases

            if (width <= 0 || height <= 0) { return; }

            var showInterface = Properties.Settings.Default.ShowInterface;

            double maxWidth, maxHeight;
            var borderSpaceHeight = showInterface ? LoadWindows.GetMainWindow.LowerBar.Height + LoadWindows.GetMainWindow.TitleBar.Height + 6 : 6;
            var borderSpaceWidth = 20 * MonitorInfo.DpiScaling; // Based on UI borders

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

            if (Properties.Settings.Default.PicGallery == 2)
            {
                /// Extra padding for picgallery required
                padding += picGalleryItem_Size - 50;
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
                    maxWidth = LoadWindows.GetMainWindow.ParentContainer.ActualWidth;
                    maxHeight = LoadWindows.GetMainWindow.ParentContainer.ActualHeight;
                }
                else
                {
                    maxWidth = Math.Min(LoadWindows.GetMainWindow.ParentContainer.ActualWidth, width);
                    maxHeight = Math.Min(LoadWindows.GetMainWindow.ParentContainer.ActualHeight, height);
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

            if (IsScrollEnabled)
            {
                /// Calculate height based on width
                LoadWindows.GetMainWindow.MainImage.Width = maxWidth;
                LoadWindows.GetMainWindow.MainImage.Height = maxWidth * height / width;

                /// Set mainWindow.Scroller height to aspect ratio calculation
                LoadWindows.GetMainWindow.Scroller.Height = height * AspectRatio;

                /// Update values
                xWidth = LoadWindows.GetMainWindow.MainImage.Width;
                xHeight = LoadWindows.GetMainWindow.Scroller.Height;
            }
            else
            {
                /// Reset mainWindow.Scroller's height to auto
                LoadWindows.GetMainWindow.Scroller.Height = double.NaN;

                /// Fit image by aspect ratio calculation
                /// and update values
                LoadWindows.GetMainWindow.MainImage.Width = xWidth = width * AspectRatio;
                LoadWindows.GetMainWindow.MainImage.Height = xHeight = height * AspectRatio;
            }

            if (!Properties.Settings.Default.Fullscreen)
            {
                /// Update TitleBar
                var interfaceSize = 190 * MonitorInfo.DpiScaling; // logo and buttons width

                if (Properties.Settings.Default.PicGallery == 2)
                {
                    if (xWidth >= monitorWidth - (UC.GetPicGallery.ActualWidth + 5) * 2.2)
                    {
                        // Offset window to not overlap gallery
                        LoadWindows.GetMainWindow.Left = ((MonitorInfo.WorkArea.Width - (UC.GetPicGallery.ActualWidth + 5) - (LoadWindows.GetMainWindow.ActualWidth * MonitorInfo.DpiScaling)) / 2)
                                          + (MonitorInfo.WorkArea.Left * MonitorInfo.DpiScaling);
                        LoadWindows.GetMainWindow.Top = ((MonitorInfo.WorkArea.Height
                                           - (LoadWindows.GetMainWindow.Height * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Top * MonitorInfo.DpiScaling);
                    }
                    else
                    {
                        CenterWindowOnScreen();
                    }
                }
                else if (Properties.Settings.Default.AutoFitWindow)
                {
                    /// Update mainWindow.TitleBar width to dynamically fit new size
                    var x = Rotateint == 0 || Rotateint == 180 ? Math.Max(xWidth, LoadWindows.GetMainWindow.MinWidth) : Math.Max(xHeight, LoadWindows.GetMainWindow.MinHeight);
                    LoadWindows.GetMainWindow.TitleText.MaxWidth = x - interfaceSize < interfaceSize ? interfaceSize : x - interfaceSize;

                    CenterWindowOnScreen();
                }
                else
                {
                    /// Fix title width to window size
                    LoadWindows.GetMainWindow.TitleText.MaxWidth = LoadWindows.GetMainWindow.ActualWidth - interfaceSize;
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