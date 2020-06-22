using PicView.ImageHandling;
using System;
using static PicView.Library.Fields;
using static PicView.UI.Sizing.WindowLogic;
using static PicView.UI.TransformImage.Scroll;

namespace PicView.UI.Sizing
{
    internal static class ScaleImage
    {
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
                    var pic = ChangeImage.Preloader.Load(Pics[FolderIndex]);
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
                        else if (mainWindow.img.Source != null)
                        {
                            FitImage(mainWindow.img.Source.Width, mainWindow.img.Source.Height);
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
            else if (mainWindow.img.Source != null)
            {
                FitImage(mainWindow.img.Source.Width, mainWindow.img.Source.Height);
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
            if (width <= 0 || height <= 0) { return; }

            var showInterface = Properties.Settings.Default.ShowInterface;

            double maxWidth, maxHeight;
            var padding = 75; // Padding to make it feel more comfortable
            var borderSpaceHeight = showInterface ? mainWindow.LowerBar.Height + mainWindow.TitleBar.Height + 6 : 6;
            var borderSpaceWidth = 20; // Based on UI borders

            var monitorWidth = MonitorInfo.Width - borderSpaceWidth;
            var monitorHeight = MonitorInfo.Height - borderSpaceHeight;

            if (Properties.Settings.Default.AutoFitWindow) // If non resizeable behaviour
            {
                if (Properties.Settings.Default.FillImage) // Max to monitor height if scaling enabled, else go by min pixel width
                {
                    maxWidth = monitorWidth;
                    maxHeight = monitorHeight;
                }
                else
                {
                    if (Properties.Settings.Default.PicGallery == 2)
                    {
                        /// Extra padding for picgallery required
                        padding += picGalleryItem_Size; // Fixes Math.Min returning incorrectly
                        maxWidth = Math.Min(monitorWidth - padding, width);
                        maxHeight = Math.Min(monitorHeight, height);
                    }
                    else if (showInterface)
                    {
                        /// Use padding for shown interface
                        maxWidth = Math.Min(monitorWidth - padding, width);
                        maxHeight = Math.Min(monitorHeight - padding, height);
                    }
                    else
                    {
                        /// Fill users screen
                        maxWidth = Math.Min(monitorWidth, width);
                        maxHeight = Math.Min(monitorHeight, height);
                    }
                }
            }
            else /// Get max width and height, based on window size
            {
                if (Properties.Settings.Default.FillImage)
                {
                    maxWidth = mainWindow.bg.ActualWidth;
                    maxHeight = mainWindow.bg.ActualHeight;
                }
                else
                {
                    maxWidth = Math.Min(mainWindow.Width, width);

                    if (showInterface)
                    {
                        /// Use padding for shown interface
                        maxHeight = Math.Min(mainWindow.Height - padding, height);
                    }
                    else
                    {
                        maxHeight = Math.Min(mainWindow.Height, height);
                    }
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
                mainWindow.img.Width = maxWidth;
                mainWindow.img.Height = maxWidth * height / width;

                /// Set mainWindow.Scroller height to aspect ratio calculation
                mainWindow.Scroller.Height = height * AspectRatio;

                /// Update values
                xWidth = mainWindow.img.Width;
                xHeight = mainWindow.Scroller.Height;
            }
            else
            {
                /// Reset mainWindow.Scroller's height to auto
                mainWindow.Scroller.Height = double.NaN;

                /// Fit image by aspect ratio calculation
                /// and update values
                mainWindow.img.Width = xWidth = width * AspectRatio;
                mainWindow.img.Height = xHeight = height * AspectRatio;
            }

            /// Update TitleBar
            var interfaceSize = 220; // logo and buttons width + extra padding
            if (Properties.Settings.Default.AutoFitWindow)
            {
                /// Update mainWindow.TitleBar width to dynamically fit new size
                var x = Rotateint == 0 || Rotateint == 180 ? xWidth : xHeight;
                mainWindow.Bar.MaxWidth = x - interfaceSize < interfaceSize ? interfaceSize : x - interfaceSize;

                // TODO Loses position gradually if not forced to center
                if (!Properties.Settings.Default.Fullscreen)
                {
                    if (Properties.Settings.Default.PicGallery == 2 && xWidth >= monitorWidth - (picGalleryItem_Size + 200))
                    {
                        // Offset window to not overlap gallery
                        mainWindow.Left = ((MonitorInfo.WorkArea.Width - picGalleryItem_Size - (mainWindow.Width * MonitorInfo.DpiScaling)) / 2)
                                          + (MonitorInfo.WorkArea.Left * MonitorInfo.DpiScaling);
                        mainWindow.Top = ((MonitorInfo.WorkArea.Height
                                           - (mainWindow.Height * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Top * MonitorInfo.DpiScaling);
                    }
                    else
                    {
                        CenterWindowOnScreen();
                    }
                }
            }
            else
            {
                /// Fix title width to window size
                mainWindow.Bar.MaxWidth = mainWindow.ActualWidth - interfaceSize;
            }

            if (IsZoomed)
            {
                TransformImage.ZoomLogic.ResetZoom();
            }

            /*

                            _.._   _..---.
                         .-"    ;-"       \
                        /      /           |
                       |      |       _=   |
                       ;   _.-'\__.-')     |
                        `-'      |   |    ;
                                 |  /;   /      _,
                               .-.;.-=-./-""-.-` _`
                              /   |     \     \-` `,
                             |    |      |     |
                             |____|______|     |
                              \0 / \0   /      /
                           .--.-""-.`--'     .'
                          (#   )          ,  \
                          ('--'          /\`  \
                           \       ,,  .'      \
                            `-._    _.'\        \
                   jgs          `""`    \        \

                   So much math!
            */
        }
    }
}