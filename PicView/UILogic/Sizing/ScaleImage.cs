using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.UILogic.TransformImage;
using System;
using System.Threading.Tasks;
using static PicView.ChangeImage.Navigation;
using static PicView.PicGallery.GalleryNavigation;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.TransformImage.Rotation;

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
                if (Pics?.Count > FolderIndex)
                {
                    var preloadValue = ChangeImage.Preloader.Get(Navigation.Pics[Navigation.FolderIndex]);
                    if (preloadValue != null)
                    {
                        var pic = preloadValue.bitmapSource;
                        if (pic != null)
                        {
                            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                            {
                                FitImage(pic.PixelWidth, pic.PixelHeight);
                            });
                            return true;
                        }
                    }
                    else
                    {
                        var size = await ImageSizeFunctions.GetImageSizeAsync(Pics[FolderIndex]).ConfigureAwait(false);
                        if (size.HasValue)
                        {
                            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                            {
                                FitImage(size.Value.Width, size.Value.Height);
                            });

                            return true;
                        }
                        else if (GetMainWindow.MainImage.Source != null)
                        {
                            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                            {
                                FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
                            });
                            return true;
                        }
                        else if (XWidth > 0 && XHeight > 0)
                        {
                            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                            {
                                FitImage(XWidth, XHeight);
                            });
                            return true;
                        }
                    }
                }
            }
            else if (XWidth > 0 && XHeight > 0)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    FitImage(XWidth, XHeight);
                });
                return true;
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    if (GetMainWindow.MainImage.Source != null)
                    {
                        FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
                        return true;
                    }
                    return false;
                });
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

            double padding = MonitorInfo.DpiScaling <= 1 ? 20 * MonitorInfo.DpiScaling : 0; // Padding to make it feel more comfortable

            if (GalleryFunctions.IsVerticalFullscreenOpen)
            {
                /// Extra padding for picgallery required
                padding += PicGalleryItem_Size - 50;
                maxWidth = Math.Min(monitorWidth - padding, width);
                maxHeight = Math.Min(monitorHeight, height);
            }
            else if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                maxWidth = Math.Min(monitorWidth - padding, width);
                maxHeight = Math.Min(monitorHeight - PicGalleryItem_Size_s, height);
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

            switch (Rotateint) // Standard aspect ratio calculation
            {
                case 0:
                case 180:
                    AspectRatio = Math.Min(maxWidth / width, maxHeight / height);
                    break;
                default:
                    AspectRatio = Math.Min(maxWidth / height, maxHeight / width);
                    break;
            }

            if (Properties.Settings.Default.ScrollEnabled)
            {
                GetMainWindow.MainImage.Height = maxWidth * height / width;
                GetMainWindow.MainImage.Width = maxWidth;

                if (Properties.Settings.Default.AutoFitWindow)
                {
                    GetMainWindow.ParentContainer.Width = maxWidth;
                    GetMainWindow.ParentContainer.Height = XHeight = height * AspectRatio;
                }
            }
            else
            {
                /// Fit image by aspect ratio calculation
                /// and update values
                GetMainWindow.MainImage.Width = XWidth = width * AspectRatio;
                GetMainWindow.MainImage.Height = XHeight = height * AspectRatio;

                GetMainWindow.ParentContainer.Width = double.NaN;
                GetMainWindow.ParentContainer.Height = double.NaN;
            }

            // Calculate window position
            if (GetMainWindow.WindowState == System.Windows.WindowState.Normal)
            {
                /// Update TitleBar maxWidth... Ugly code, but it works. Binding to ParentContainer.ActualWidth depends on correct timing.
                var interfaceSize = (GetMainWindow.Logo.ActualWidth + 13) + GetMainWindow.MinButton.ActualWidth
                    + GetMainWindow.FullscreenButton.ActualWidth + GetMainWindow.CloseButton.ActualWidth * MonitorInfo.DpiScaling;

                var autoWidth = Properties.Settings.Default.AutoFitWindow ? GetMainWindow.ActualWidth : XWidth;
                var autoHeight = Properties.Settings.Default.AutoFitWindow ? GetMainWindow.ActualHeight : XHeight;

                if (GalleryFunctions.IsVerticalFullscreenOpen)
                {
                    GetMainWindow.Top = ((MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - autoHeight) / 2 + MonitorInfo.WorkArea.Top;
                    GetMainWindow.Left = ((MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - (autoWidth + UC.GetPicGallery.Width)) / 2 + MonitorInfo.WorkArea.Left;
                }
                else if (GalleryFunctions.IsHorizontalFullscreenOpen)
                {
                    GetMainWindow.Top = ((MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - (autoHeight + GalleryNavigation.PicGalleryItem_Size + UC.GetPicGallery.Margin.Bottom + 4 * MonitorInfo.DpiScaling)) / 2 + MonitorInfo.WorkArea.Top;
                    GetMainWindow.Left = ((MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - autoWidth) / 2 + MonitorInfo.WorkArea.Left;

                }
                else if (Properties.Settings.Default.AutoFitWindow)
                {
                    if (Properties.Settings.Default.KeepCentered)
                    {
                        CenterWindowOnScreen();
                    }

                    // Update mainWindow.TitleBar width to dynamically fit new size
                    var x = Rotateint == 0 || Rotateint == 180 ? Math.Max(XWidth, GetMainWindow.MinWidth) : Math.Max(XHeight, GetMainWindow.MinHeight);
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        GetMainWindow.TitleText.MaxWidth = x;
                    }
                    else
                    {
                        GetMainWindow.TitleText.MaxWidth = x - interfaceSize < interfaceSize ? interfaceSize : x - interfaceSize;
                    }
                }
                else
                {
                    /// Fix title width to window size
                    GetMainWindow.TitleText.MaxWidth = GetMainWindow.ActualWidth - interfaceSize;
                }
            }

            if (ZoomLogic.translateTransform is not null && ZoomLogic.translateTransform?.X != 0d)
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