using System.Windows;
using PicView.ChangeImage;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic.TransformImage;
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
        /// Tries to call FitImage with additional error checking
        /// </summary>
        internal static void TryFitImage()
        {
            GetMainWindow.Dispatcher.Invoke(() =>
            {
                if (ErrorHandling.CheckOutOfRange() == false)
                {
                    var preloadValue = Preloader.Get(FolderIndex);
                    if (preloadValue == null)
                    {
                        if (XWidth > 0 && XHeight > 0)
                        {
                            FitImage(XWidth, XHeight);
                        }
                        return;
                    }
                    var pic = preloadValue.BitmapSource;
                    if (pic != null)
                    {
                        FitImage(pic.PixelWidth, pic.PixelHeight);
                    }
                }
                else if (XWidth > 0 && XHeight > 0)
                {
                    FitImage(XWidth, XHeight);
                }
                else if (GetMainWindow.MainImage.Source != null)
                {
                    FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
                }
            });
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
            var borderSpaceHeight = Settings.Default.Fullscreen ? 0 : GetMainWindow.LowerBar.Height + GetMainWindow.TitleBar.Height + 6;
            var borderSpaceWidth = Settings.Default is {Fullscreen: true, ShowAltInterfaceButtons: true} ? 0 : 20 * MonitorInfo.DpiScaling;

            var monitorWidth = (MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - borderSpaceWidth;
            var monitorHeight = (MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - borderSpaceHeight;

            var padding = MonitorInfo.DpiScaling <= 1 ? 20 * MonitorInfo.DpiScaling : 0; // Padding to make it feel more comfortable
            var margin = 0d;

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                maxWidth = Settings.Default.FillImage ? monitorWidth : Math.Min(monitorWidth - padding, width);
                maxHeight = Settings.Default.FillImage ? monitorHeight - 40 : Math.Min(monitorHeight - PicGalleryItemSize, height);
                margin = PicGalleryItemSize + 5;
            }
            else if (Settings.Default.AutoFitWindow)
            {
                maxWidth = Settings.Default.FillImage ? monitorWidth : Math.Min(monitorWidth - padding, width);
                maxHeight = Settings.Default.FillImage ? monitorHeight : Math.Min(monitorHeight - padding, height);
            }
            else
            {
                maxWidth = Settings.Default.FillImage ?
                    GetMainWindow.ParentContainer.ActualWidth : Math.Min(GetMainWindow.ParentContainer.ActualWidth, width);
                if (Settings.Default.ScrollEnabled)
                {
                    maxHeight = Settings.Default.FillImage ?
                        GetMainWindow.ParentContainer.ActualHeight : height;
                }
                else
                {
                    maxHeight = Settings.Default.FillImage ?
                        GetMainWindow.ParentContainer.ActualHeight : Math.Min(GetMainWindow.ParentContainer.ActualHeight, height);
                }
            }

            switch (RotationAngle) // aspect ratio calculation
            {
                case 0:
                case 180:
                    AspectRatio = Math.Min(maxWidth / width, maxHeight / height);
                    break;

                case 90:
                case 270:
                    AspectRatio = Math.Min(maxWidth / height, maxHeight / width);
                    break;

                default:
                    var rotationRadians = RotationAngle * Math.PI / 180;
                    var newWidth = Math.Abs(width * Math.Cos(rotationRadians)) + Math.Abs(height * Math.Sin(rotationRadians));
                    var newHeight = Math.Abs(width * Math.Sin(rotationRadians)) + Math.Abs(height * Math.Cos(rotationRadians));
                    AspectRatio = Math.Min(maxWidth / newWidth, maxHeight / newHeight);
                    break;
            }

            if (Settings.Default.ScrollEnabled)
            {
                GetMainWindow.MainImage.Height = maxWidth * height / width;
                GetMainWindow.MainImage.Width = maxWidth;

                if (Settings.Default.AutoFitWindow)
                {
                    GetMainWindow.ParentContainer.Width = maxWidth;
                    GetMainWindow.ParentContainer.Height = XHeight = height * AspectRatio;
                }
            }
            else
            {
                // Fit image by aspect ratio calculation
                // and update values
                GetMainWindow.MainImage.Width = XWidth = width * AspectRatio;
                GetMainWindow.MainImage.Height = XHeight = height * AspectRatio;

                GetMainWindow.ParentContainer.Width = double.NaN;
                GetMainWindow.ParentContainer.Height = double.NaN;
            }

            // Update margin when from fullscreen gallery and when not
            GetMainWindow.MainImage.Margin = new Thickness(0, 0, 0, margin);

            if (ZoomLogic.IsZoomed)
            {
                ZoomLogic.ResetZoom(false);
            }

            if (GalleryFunctions.IsHorizontalFullscreenOpen) return;

            if (Settings.Default.AutoFitWindow)
            {
                CenterWindowOnScreen(Settings.Default.KeepCentered); // Vertically center or vertically and horizontally center

                GetMainWindow.TitleText.MaxWidth = maxWidth;
            }
            else
            {
                // Update TitleBar maxWidth... Ugly code, but it works. Binding to ParentContainer.ActualWidth depends on correct timing.
                var interfaceSize =
                    GetMainWindow.Logo.Width + GetMainWindow.GalleryButton.Width + GetMainWindow.RotateButton.Width + GetMainWindow.RotateButton.Width
                    + GetMainWindow.MinButton.Width + GetMainWindow.FullscreenButton.Width + GetMainWindow.CloseButton.Width;

                // Fix title width to window size
                GetMainWindow.TitleText.MaxWidth = GetMainWindow.ActualWidth - interfaceSize;
            }
        }

        //
        //
        //               _.._   _..---.
        //             .-"    ;-"       \
        //            /      /           |
        //           |      |       _=   |
        //           ;   _.-'\__.-')     |
        //            `-'      |   |    ;
        //                    |  /;   /      _,
        //                  .-.;.-=-./-""-.-` _`
        //                 /   |     \     \-` `,
        //                |    |      |     |
        //                |____|______|     |
        //                 \0 / \0   /      /
        //              .--.-""-.`--'     .'
        //             (#   )          ,  \
        //             ('--'          /\`  \
        //              \       ,,  .'      \
        //               `-._    _.'\        \
        //      jgs          `""`    \        \
        //
        //      So much math!
        //
    }
}