namespace PicView.Core.Sizing;

public static class ImageSizeCalculationHelper
{
    public static Fit GetImageSize(
        double imageWidth,
        double imageHeight,
        ScreenSize screenSize,
        double containerWidth,
        double containerHeight,
        double rotationAngle,
        double uiTopSize,
        double uiBottomSize,
        double galleryWidth,
        double galleryHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return new Fit(0, 0, 0, 0, 0,  0, 0);
        }
        
        double maxAvailableWidth, maxAvailableHeight;
        if (Settings.ImageScaling.ZoomToFit)
        {
            var (w, h) = GetMaxAvailableScreenSize(screenSize,  containerWidth, containerHeight, uiTopSize, uiBottomSize, galleryWidth, galleryHeight);
            maxAvailableWidth = w;
            maxAvailableHeight = h;
        }
        else
        {
            var (w, h) = GetMaxAvailableImageSize(screenSize, containerWidth, containerHeight,  imageWidth, imageHeight, uiTopSize, uiBottomSize, galleryWidth, galleryHeight);
            maxAvailableWidth = w;
            maxAvailableHeight = h;
        }

        var aspectRatio = CalculateAspectRatio(rotationAngle, maxAvailableWidth, maxAvailableHeight, imageWidth, imageHeight);


        double scrollWidth, scrollHeight;
        double windowWidth, windowHeight;
        double calculatedImageWidth, calculatedImageHeight;

        var isRotated = rotationAngle is 90 or 270;
        
        if (Settings.Zoom.ScrollEnabled)
        {
            // Calculate aspect ratio that fits the image to the available width, 
            // but ignore the height constraint to allow for scrolling.
            aspectRatio = CalculateAspectRatio(rotationAngle, maxAvailableWidth, double.PositiveInfinity, imageWidth, imageHeight);

            calculatedImageWidth = imageWidth * aspectRatio;
            calculatedImageHeight = imageHeight * aspectRatio;

            scrollWidth = Math.Min(calculatedImageWidth, maxAvailableWidth);
            scrollHeight = Math.Min(calculatedImageHeight, maxAvailableHeight);

            if (isRotated)
            {
                windowWidth = Math.Max(scrollHeight + uiBottomSize + uiTopSize + galleryHeight, SizeDefaults.WindowMinSize); 
                windowHeight = Math.Max(scrollWidth + galleryWidth, SizeDefaults.WindowMinSize);   
            }
            else
            {
                windowWidth = Math.Max(scrollWidth + galleryWidth, SizeDefaults.WindowMinSize);
                windowHeight = Math.Max(scrollHeight + uiBottomSize + uiTopSize + galleryHeight, SizeDefaults.WindowMinSize);
            }
        }
        else
        {
            calculatedImageWidth = imageWidth * aspectRatio;
            calculatedImageHeight = imageHeight * aspectRatio;

            scrollWidth = double.NaN;
            scrollHeight = double.NaN;

            if (isRotated)
            {
                windowWidth = Math.Max(calculatedImageHeight + galleryWidth, SizeDefaults.WindowMinSize);
                windowHeight = Math.Max(calculatedImageWidth + uiBottomSize + uiTopSize + galleryHeight, SizeDefaults.WindowMinSize);
            }
            else
            {
                windowWidth = Math.Max(calculatedImageWidth + galleryWidth, SizeDefaults.WindowMinSize);
                windowHeight = Math.Max(calculatedImageHeight + uiBottomSize + uiTopSize + galleryHeight, SizeDefaults.WindowMinSize);
            }
        }

        return new Fit(windowWidth, windowHeight, calculatedImageWidth, calculatedImageHeight, 
            scrollWidth, scrollHeight, aspectRatio);
    }

    private static double CalculateAspectRatio(double rotationAngle, double maxWidth, double maxHeight, double width,
        double height)
    {
        switch (rotationAngle)
        {
            case 0:
            case 180:
                return Math.Min(maxWidth / width, maxHeight / height);
            case 90:
            case 270:
                return Math.Min(maxWidth / height, maxHeight / width);
            default:
                var radians = rotationAngle * Math.PI / 180;
                var rotatedWidth = Math.Abs(width * Math.Cos(radians)) + Math.Abs(height * Math.Sin(radians));
                var rotatedHeight = Math.Abs(width * Math.Sin(radians)) + Math.Abs(height * Math.Cos(radians));
                return Math.Min(maxWidth / rotatedWidth, maxHeight / rotatedHeight);
        }
    }

    private static (double maxWidth, double maxHeight) GetMaxAvailableScreenSize(
        ScreenSize screenSize, double containerWidth, double containerHeight, double uiTopSize, double uiBottomSize, double galleryWidth, double galleryHeight)
    {
        if (Settings.WindowProperties.Fullscreen)
        {
            return (Math.Max(0, screenSize.Width - galleryWidth), 
                Math.Max(0, screenSize.Height - galleryHeight));
        }

        if (!Settings.WindowProperties.AutoFit)
        {
            return (Math.Max(0, containerWidth - galleryWidth), Math.Max(0, containerHeight - galleryHeight));
        }
        // Calculate the absolute maximum allowed window size (Working area minus the margin)
        var maxWindowWidth = screenSize.WorkingAreaWidth - Settings.WindowProperties.Margin;
        var maxWindowHeight = screenSize.WorkingAreaHeight - Settings.WindowProperties.Margin;
            
        // The available space specifically for the *image* is the max window size minus UI elements
        var maxAvailableWidth = maxWindowWidth - galleryWidth;
        var maxAvailableHeight = maxWindowHeight - (galleryHeight + uiBottomSize + uiTopSize);

        // Ensure we don't return negative values if the UI is somehow larger than the screen bounds
        return (Math.Max(0, maxAvailableWidth), Math.Max(0, maxAvailableHeight));
    }

    private static (double maxWidth, double maxHeight) GetMaxAvailableImageSize(
        ScreenSize screenSize, double containerWidth, double containerHeight, double imageWidth, double imageHeight, double uiTopSize, double uiBottomSize, double galleryWidth, double galleryHeight)
    {
        // 1. Get the absolute maximum space the screen allows for an image
        var (screenMaxWidth, screenMaxHeight) = GetMaxAvailableScreenSize(screenSize, containerWidth, containerHeight, uiTopSize, uiBottomSize, galleryWidth, galleryHeight);

        // 2. Bound that available space by the native image resolution. 
        // This ensures the aspect ratio calculation never scales the image beyond 100%.
        var maxAvailableWidth = Math.Min(screenMaxWidth, imageWidth);
        var maxAvailableHeight = Math.Min(screenMaxHeight, imageHeight);

        return (maxAvailableWidth, maxAvailableHeight);
    }

    public static Fit GetSideBySideImageSize(
        double width,
        double height,
        double secondaryWidth,
        double secondaryHeight,
        ScreenSize screenSize,
        double containerWidth,
        double containerHeight,
        double rotationAngle,
        double uiTopSize,
        double uiBottomSize,
        double galleryWidth,
        double galleryHeight)
    {
        if (width <= 0 || height <= 0 || secondaryWidth <= 0 || secondaryHeight <= 0)
        {
            return new Fit(0, 0, 0, 0, 0, 0, 0);
        }

        // 1. Normalize the widths so both images mathematically share the largest height.
        // This guarantees they will scale evenly and utilize the maximum available container space.
        var largestHeight = Math.Max(height, secondaryHeight);
        var normalizedWidth = width * (largestHeight / height);
        var normalizedSecondaryWidth = secondaryWidth * (largestHeight / secondaryHeight);
        
        // 2. Combine the normalized widths to create our perfectly balanced virtual bounding box.
        var combinedWidth = normalizedWidth + normalizedSecondaryWidth;

        double maxAvailableWidth, maxAvailableHeight;
        if (Settings.WindowProperties.AutoFit)
        {
            var (w, h) = GetMaxAvailableScreenSize(screenSize, containerWidth, containerHeight, uiTopSize, uiBottomSize, galleryWidth, galleryHeight);
            maxAvailableWidth = w;
            maxAvailableHeight = h;
        }
        else
        {
            maxAvailableWidth = containerWidth;
            maxAvailableHeight = containerHeight;
        }
        
        // 3. Calculate the aspect ratio to fit this evenly balanced bounding box into the screen/container.
        var aspectRatio = CalculateAspectRatio(rotationAngle, maxAvailableWidth, maxAvailableHeight, combinedWidth, largestHeight);

        // 4. Apply the scaling factor to our virtual image.
        double calculatedCombinedWidth, calculatedLargestHeight;
        double scrollWidth, scrollHeight;
        double windowWidth, windowHeight;

        if (Settings.Zoom.ScrollEnabled)
        {
            aspectRatio = CalculateAspectRatio(rotationAngle, maxAvailableWidth, double.PositiveInfinity, combinedWidth, largestHeight);

            calculatedCombinedWidth = combinedWidth * aspectRatio;
            calculatedLargestHeight = largestHeight * aspectRatio;

            scrollWidth = Math.Min(calculatedCombinedWidth, maxAvailableWidth);
            scrollHeight = Math.Min(calculatedLargestHeight, maxAvailableHeight);

            windowWidth = scrollWidth + galleryWidth;
            windowHeight = scrollHeight + uiBottomSize + uiTopSize + galleryHeight;
        }
        else
        {
            calculatedCombinedWidth = combinedWidth * aspectRatio;
            calculatedLargestHeight = largestHeight * aspectRatio;

            scrollWidth = double.NaN;
            scrollHeight = double.NaN;

            windowWidth = calculatedCombinedWidth + galleryWidth;
            windowHeight = calculatedLargestHeight + uiBottomSize + uiTopSize + galleryHeight;
        }

        return new Fit(
            windowWidth, 
            windowHeight, 
            calculatedCombinedWidth, 
            calculatedLargestHeight, 
            scrollWidth, 
            scrollHeight, 
            aspectRatio);
    }
}