using PicView.ChangeImage;
using PicView.Properties;
using PicView.UILogic.TransformImage;
using System.Windows;
using PicView.PicGallery;
using static PicView.ChangeImage.Navigation;
using static PicView.PicGallery.GalleryNavigation;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.TransformImage.Rotation;

namespace PicView.UILogic.Sizing;

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
                var preloadValue = PreLoader.Get(FolderIndex);
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
        var margin = 0d;
        var padding = MonitorInfo.DpiScaling <= 1 ? 20 * MonitorInfo.DpiScaling : 0; // Padding to make it feel more comfortable
        var isFullScreenSize = Settings.Default.Fullscreen;

        var galleryHeight = 0.0;
        if (UC.GetPicGallery is not null)
        {
            if (Settings.Default.IsBottomGalleryShown)
            {
                galleryHeight = PicGalleryItemSize + 22;
            }
        }
        var borderSpaceHeight = isFullScreenSize ? 0 : GetMainWindow.LowerBar.ActualHeight + GetMainWindow.TitleBar.ActualHeight + galleryHeight;
        var borderSpaceWidth = Settings.Default.Fullscreen ? 0 : padding;

        var workAreaWidth = (MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - borderSpaceWidth;
        var workAreaHeight = (MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - borderSpaceHeight;

        if (Settings.Default.AutoFitWindow)
        {
            maxWidth = Settings.Default.FillImage ? workAreaWidth : Math.Min(workAreaWidth - padding, width);
            maxHeight = Settings.Default.FillImage ? workAreaHeight : Math.Min(workAreaHeight - padding, height);
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
                    GetMainWindow.ParentContainer.ActualHeight - galleryHeight : Math.Min(GetMainWindow.ParentContainer.ActualHeight - galleryHeight, height);
            }
        }

        if (Settings.Default.IsBottomGalleryShown) // Set to if new gallery opened and Settings.Default.FullscreenGallery
        {
            if (PicGalleryItemSize is 0)
            {
                SetSize(Settings.Default.BottomGalleryItemSize);
            }
            margin = PicGalleryItemSize + 22; // Scrollbar
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

        if (width * AspectRatio < 0 || height * AspectRatio < 0) return; // Fix weird error when entering fullscreen gallery

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

            if (Settings.Default.IsBottomGalleryShown && UC.GetPicGallery is not null)
            {
                if (Settings.Default.AutoFitWindow)
                {
                    UC.GetPicGallery.Width = XWidth;
                }
                else if (!double.IsNaN(GetMainWindow.ParentContainer.Width))
                {
                    UC.GetPicGallery.Width = GetMainWindow.ParentContainer.Width;
                }
            }
        }

        // Update margin when from fullscreen gallery and when not
        GetMainWindow.MainImage.Margin = new Thickness(0, 0, 0, margin);

        if (ZoomLogic.IsZoomed)
        {
            ZoomLogic.ResetZoom(false);
        }

        if (isFullScreenSize) return;

        // Update TitleBar maxWidth... Ugly code, but it works. Binding to ParentContainer.ActualWidth depends on correct timing.
        var interfaceSize =
            GetMainWindow.Logo.Width + GetMainWindow.GalleryButton.Width + GetMainWindow.RotateButton.Width + GetMainWindow.RotateButton.Width
            + GetMainWindow.MinButton.Width + GetMainWindow.FullscreenButton.Width + GetMainWindow.CloseButton.Width;

        if (Settings.Default.AutoFitWindow)
        {
            CenterWindowOnScreen(Settings.Default.KeepCentered); // Vertically center or vertically and horizontally center

            var titleBarMaxWidth = RotationAngle is 0 or 180 ?
                Math.Max(XWidth, GetMainWindow.MinWidth) : Math.Max(XHeight, GetMainWindow.MinHeight);

            if (Settings.Default.ScrollEnabled)
            {
                GetMainWindow.TitleText.MaxWidth = titleBarMaxWidth;
            }
            else
            {
                GetMainWindow.TitleText.MaxWidth = titleBarMaxWidth - interfaceSize < interfaceSize ?
                    interfaceSize : titleBarMaxWidth - interfaceSize;
            }
        }
        else
        {
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