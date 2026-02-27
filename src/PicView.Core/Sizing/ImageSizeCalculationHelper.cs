namespace PicView.Core.Sizing;

public static class ImageSizeCalculationHelper
{
    private const int MinTitleWidth = 250;

    /// <summary>
    /// Calculates the dimensions of how the image should fit within the window, based on current settings and parameters
    /// </summary>
    /// <param name="imageWidth">The original pixel width of the image.</param>
    /// <param name="imageHeight">The original pixel height of the image.</param>
    /// <param name="screenSize">The dimensions of the screen or display area.</param>
    /// <param name="minWidth">The minimum allowable width of the window.</param>
    /// <param name="minHeight">The minimum allowable height of the window.</param>
    /// <param name="interfaceSize">The combined width of the buttons in the titlebar</param>
    /// <param name="dpiScaling">The scaling factor accounting for DPI.</param>
    /// <param name="uiTopSize">The height of the user interface at the top of the display.</param>
    /// <param name="uiBottomSize">The height of the user interface at the bottom of the display.</param>
    /// <param name="galleryHeight">The height of the bottom gallery area, if displayed.</param>
    /// <param name="containerWidth">The width of the container in which the image is displayed.</param>
    /// <param name="containerHeight">The height of the container in which the image is displayed.</param>
    /// <returns>An instance of <see cref="ImageSize"/> containing the calculated dimensions and related properties for the image.</returns>
    public static ImageSize GetImageSize(
        double imageWidth,
        double imageHeight,
        ScreenSize screenSize,
        double minWidth,
        double minHeight,
        double interfaceSize,
        double dpiScaling,
        double uiTopSize,
        double uiBottomSize,
        double galleryHeight,
        double containerWidth,
        double containerHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return ErrorImageSize(minWidth, minHeight, interfaceSize, containerWidth);
        }

        // When in fullscreen, we need to capture the entire screen estate
        var isFullscreen = Settings.WindowProperties.Fullscreen;
        // When in maximized mode, working area and interface size needs to be taken into consideration
        var isMaximized = Settings.WindowProperties.Maximized;

        var scrollEnabled = Settings.Zoom.ScrollEnabled;
        var stretchImage = Settings.ImageScaling.StretchImage;
        var autoFit = Settings.WindowProperties.AutoFit;
        var showBottomGallery = Settings.Gallery.IsBottomGalleryShown;
        var showInterface = Settings.UIProperties.ShowInterface;
        var showGalleryInHiddenUI = Settings.Gallery.ShowBottomGalleryInHiddenUI;

        // Calculate the possible surrounding area and borders between the picture and window
        var borderSpaceHeight = CalculateBorderSpaceHeight(isFullscreen, uiTopSize, uiBottomSize, galleryHeight);
        var borderSpaceWidth = isFullscreen  ? 0 : screenSize.Margin;

        var workArea = CalculateWorkArea(screenSize, isFullscreen, borderSpaceWidth, borderSpaceHeight);
        var screenMargin = isFullscreen || isMaximized ? 0 : screenSize.Margin;

        var (maxAvailableWidth, maxAvailableHeight, adjustedContainerWidth, adjustedContainerHeight) =
            CalculateMaxImageSize(scrollEnabled, stretchImage, autoFit,
                workArea.width, workArea.height, screenMargin, imageWidth, imageHeight, dpiScaling, galleryHeight,
                containerWidth, containerHeight);

        var margin = CalculateGalleryMargin(showBottomGallery,
            showInterface, showGalleryInHiddenUI, galleryHeight);

        var aspectRatio =
            CalculateAspectRatio(maxAvailableWidth, maxAvailableHeight, imageWidth, imageHeight);

        double displayedWidth, displayedHeight, scrollWidth, scrollHeight;
        if (scrollEnabled)
        {
            (displayedWidth, displayedHeight, scrollWidth, scrollHeight) = CalculateScrolledImageSize(
                isFullscreen, autoFit, screenSize, imageWidth, imageHeight, aspectRatio,
                adjustedContainerWidth, adjustedContainerHeight, containerHeight, margin, dpiScaling
            );
        }
        else
        {
            displayedWidth = imageWidth * aspectRatio;
            displayedHeight = imageHeight * aspectRatio;
            scrollWidth = double.NaN;
            scrollHeight = double.NaN;
        }

        var titleMaxWidth = GetTitleMaxWidth(displayedWidth, displayedHeight, minWidth, minHeight,
            interfaceSize, containerWidth);
        return new ImageSize(displayedWidth, displayedHeight, 0, scrollWidth, scrollHeight, titleMaxWidth, margin,
            aspectRatio);
    }

    private static (double width, double height) CalculateWorkArea(ScreenSize screenSize, bool fullscreen,
        double borderSpaceWidth, double borderSpaceHeight)
    {
        if (fullscreen)
        {
            return (screenSize.Width, screenSize.Height);
        }

        return (screenSize.WorkingAreaWidth - borderSpaceWidth,
            screenSize.WorkingAreaHeight - borderSpaceHeight);
    }


    private static double CalculateBorderSpaceHeight(bool fullscreen, double uiTop, double uiBottom, double gallery)
        => fullscreen ? 0 : uiTop + uiBottom + gallery;

    private static (double maxWidth, double maxHeight, double containerWidth, double containerHeight)
        CalculateMaxImageSize(
            bool scrollEnabled, bool stretchImage, bool autoFit,
            double workAreaWidth, double workAreaHeight, double margin,
            double width, double height, double dpiScaling, double galleryHeight, double containerWidth,
            double containerHeight)
    {
        if (scrollEnabled)
        {
            workAreaWidth -= SizeDefaults.ScrollbarSize * dpiScaling;
            containerWidth -= SizeDefaults.ScrollbarSize * dpiScaling;
            return (stretchImage ? workAreaWidth : Math.Min(workAreaWidth - margin, width), workAreaHeight,
                containerWidth, containerHeight);
        }

        // ReSharper disable once InvertIf
        if (autoFit)
        {
            var mw = stretchImage ? workAreaWidth - margin : Math.Min(workAreaWidth - margin, width);
            var mh = stretchImage ? workAreaHeight - margin : Math.Min(workAreaHeight - margin, height);
            return (mw, mh, containerWidth, containerHeight);
        }

        return (
            stretchImage ? containerWidth : Math.Min(containerWidth, width),
            stretchImage ? containerHeight - galleryHeight : Math.Min(containerHeight - galleryHeight, height),
            containerWidth, containerHeight
        );
    }

    private static double CalculateAspectRatio(double maxWidth, double maxHeight, double width, double height)
    {
        var rotationAngle = 0;
        var radians = rotationAngle * Math.PI / 180;
        var rotatedWidth = Math.Abs(width * Math.Cos(radians)) + Math.Abs(height * Math.Sin(radians));
        var rotatedHeight = Math.Abs(width * Math.Sin(radians)) + Math.Abs(height * Math.Cos(radians));
        return Math.Min(maxWidth / rotatedWidth, maxHeight / rotatedHeight);
        
    }

    private static double CalculateGalleryMargin(bool isBottomGalleryShown, bool showInterface,
        bool showGalleryInHidden, double galleryHeight)
    {
        if (!isBottomGalleryShown)
        {
            return 0;
        }

        if (!showInterface)
        {
            return showGalleryInHidden && galleryHeight > 0 ? galleryHeight : 0;
        }

        return galleryHeight > 0 ? galleryHeight : 0;
    }

    private static (double width, double height, double scrollWidth, double scrollHeight) CalculateScrolledImageSize(
        bool fullscreen, bool autoFit, ScreenSize screenSize, double width, double height, double aspectRatio,
        double containerWidth, double containerHeight, double origContainerHeight, double margin, double dpiScaling)
    {
        if (fullscreen)
        {
            return (width * aspectRatio, height * aspectRatio, screenSize.Width, screenSize.Height);
        }

        if (autoFit)
        {
            var imgWidth = width * aspectRatio;
            var imgHeight = height * aspectRatio;
            var sw = Math.Max(imgWidth + SizeDefaults.ScrollbarSize,
                SizeDefaults.WindowMinSize + SizeDefaults.ScrollbarSize + screenSize.Margin + 16);
            var sh = origContainerHeight - margin;
            return (imgWidth, imgHeight, sw, sh);
        }

        var cWidth = containerWidth - SizeDefaults.ScrollbarSize + 10;
        var cHeight = height / width * cWidth;
        var sWidth = containerWidth + SizeDefaults.ScrollbarSize;
        var sHeight = origContainerHeight - margin;
        return (cWidth, cHeight, sWidth, sHeight);
    }


    /// <summary>
    /// Calculates the size when displaying two images side by side, where both images will scale to the same height
    /// while respecting the aspect ratio.
    /// </summary>
    /// <param name="width">The original width of the first image.</param>
    /// <param name="height">The original height of the first image.</param>
    /// <param name="secondaryWidth">The original width of the second image.</param>
    /// <param name="secondaryHeight">The original height of the second image.</param>
    /// <param name="screenSize">The dimensions of the screen.</param>
    /// <param name="minWidth">The minimum allowable width of the window.</param>
    /// <param name="minHeight">The minimum allowable height of the window.</param>
    /// <param name="interfaceSize">The combined width of the buttons in the title bar.</param>
    /// <param name="dpiScaling">The scaling factor for DPI adjustments.</param>
    /// <param name="uiTopSize">The height of the top user interface elements.</param>
    /// <param name="uiBottomSize">The height of the bottom user interface elements.</param>
    /// <param name="galleryHeight">The height of the bottom gallery area, if displayed.</param>
    /// <param name="containerWidth">The width of the container in which the images are displayed.</param>
    /// <param name="containerHeight">The height of the container in which the images are displayed.</param>
    /// <returns>An instance of <see cref="ImageSize"/> containing the calculated dimensions and related properties for displaying both images side by side.</returns>
    public static ImageSize GetSideBySideImageSize(
        double width,
        double height,
        double secondaryWidth,
        double secondaryHeight,
        ScreenSize screenSize,
        double minWidth,
        double minHeight,
        double interfaceSize,
        double dpiScaling,
        double uiTopSize,
        double uiBottomSize,
        double galleryHeight,
        double containerWidth,
        double containerHeight)
    {
        if (width <= 0 || height <= 0 || secondaryWidth <= 0 || secondaryHeight <= 0)
        {
            return ErrorImageSize(minWidth, minHeight, interfaceSize, containerWidth);
        }

        // Get sizes for both images
        var firstSize = GetImageSize(width, height, screenSize, minWidth, minHeight,
            interfaceSize, dpiScaling, uiTopSize, uiBottomSize, galleryHeight,
            containerWidth,
            containerHeight);
        var secondSize = GetImageSize(secondaryWidth, secondaryHeight, screenSize, minWidth,
            minHeight, interfaceSize, dpiScaling, uiTopSize, uiBottomSize,
            galleryHeight,
            containerWidth, containerHeight);

        // Determine maximum height for both images
        var xHeight = Math.Max(firstSize.Height, secondSize.Height);

        // Recalculate the widths to maintain the aspect ratio with the new maximum height
        var xWidth1 = firstSize.Width / firstSize.Height * xHeight;
        var xWidth2 = secondSize.Width / secondSize.Height * xHeight;

        // Combined width of both images
        var combinedWidth = xWidth1 + xWidth2;

        if (Settings.WindowProperties.AutoFit)
        {
            var widthPadding = Settings.ImageScaling.StretchImage ? 4 : screenSize.Margin;
            var availableWidth = screenSize.WorkingAreaWidth - widthPadding;
            var availableHeight = screenSize.WorkingAreaHeight - (widthPadding + uiBottomSize + uiTopSize);
            
            // If combined width exceeds available width, scale both images down proportionally
            if (combinedWidth > availableWidth)
            {
                var scaleFactor = availableWidth / combinedWidth;
                xWidth1 *= scaleFactor;
                xWidth2 *= scaleFactor;
                xHeight *= scaleFactor;

                combinedWidth = xWidth1 + xWidth2;
            }
            
        }
        else
        {
            if (combinedWidth > containerWidth)
            {
                var scaleFactor = containerWidth / combinedWidth;
                xWidth1 *= scaleFactor;
                xWidth2 *= scaleFactor;
                xHeight *= scaleFactor;

                combinedWidth = xWidth1 + xWidth2;
            }
        }

        double scrollWidth, scrollHeight;
        if (Settings.Zoom.ScrollEnabled)
        {
            if (Settings.WindowProperties.AutoFit)
            {
                combinedWidth -= SizeDefaults.ScrollbarSize;
                scrollWidth = combinedWidth + SizeDefaults.ScrollbarSize + 8;

                var fullscreen = Settings.WindowProperties.Fullscreen ||
                                 Settings.WindowProperties.Maximized;
                var borderSpaceHeight = fullscreen ? 0 : uiTopSize + uiBottomSize + galleryHeight;
                var workAreaHeight = screenSize.WorkingAreaHeight * dpiScaling - borderSpaceHeight;
                scrollHeight = Math.Min(xHeight,
                    Settings.ImageScaling.StretchImage ? workAreaHeight : workAreaHeight - screenSize.Margin);
            }
            else
            {
                combinedWidth -= SizeDefaults.ScrollbarSize + 8;
                scrollWidth = double.NaN;
                scrollHeight = double.NaN;
            }
        }
        else
        {
            scrollWidth = double.NaN;
            scrollHeight = double.NaN;
        }

        var titleMaxWidth = GetTitleMaxWidth(combinedWidth, xHeight, minWidth, minHeight, interfaceSize, containerWidth);

        var margin = firstSize.Height > secondSize.Height ? firstSize.Margin : secondSize.Margin;
        return new ImageSize(combinedWidth, xHeight, xWidth2, scrollWidth, scrollHeight, titleMaxWidth, margin,
            firstSize.AspectRatio);
    }


    // Calculate the window's title max width between the interfaceSize (the combined width of the buttons) and the images' own size
    public static double GetTitleMaxWidth(double width, double height, double monitorMinWidth,
        double monitorMinHeight, double interfaceSize, double containerWidth)
    {
        double titleMaxWidth;

        if (Settings.WindowProperties.AutoFit && !Settings.WindowProperties.Maximized)
        {
            titleMaxWidth = Math.Max(width, monitorMinWidth);

            titleMaxWidth = titleMaxWidth - interfaceSize < MinTitleWidth
                ? MinTitleWidth
                : titleMaxWidth - interfaceSize;
        }
        else
        {
            // Fix title width to window size
            titleMaxWidth = containerWidth - interfaceSize;
        }

        if (!Settings.Zoom.ScrollEnabled)
        {
            return titleMaxWidth;
        }

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            return titleMaxWidth + (SizeDefaults.ScrollbarSize + 10);
        }

        return titleMaxWidth + SizeDefaults.ScrollbarSize;
    }

    private static ImageSize ErrorImageSize(double monitorMinWidth, double monitorMinHeight, double interfaceSize, double containerWidth)
        => new(0, 0, 0, 0, 0, GetTitleMaxWidth(0, 0, monitorMinWidth, monitorMinHeight, interfaceSize, containerWidth), 0, 0);
}