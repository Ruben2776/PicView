namespace PicView.Core.Sizing;

/// <summary>
/// Fits an image, or a side-by-side pair, into the available area and works out the window size AutoFit needs,
/// before the window resizes.
/// </summary>
public static class ImageFitter
{
    public static Fit Fit(FitInput input)
    {
        if (input.SecondImageWidth > 0 && input.SecondImageHeight > 0)
        {
            return FitPair(input);
        }

        return FitSingle(input);
    }

    private static Fit FitSingle(FitInput input)
    {
        var imageWidth = input.ImageWidth;
        var imageHeight = input.ImageHeight;
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return default;
        }

        var reserved = input.ReservedSpace;
        var (maxAvailableWidth, maxAvailableHeight) = GetAvailableArea(input);
        if (!input.ZoomToFit)
        {
            // Never scale the image beyond 100%
            maxAvailableWidth = Math.Min(maxAvailableWidth, imageWidth);
            maxAvailableHeight = Math.Min(maxAvailableHeight, imageHeight);
        }

        var isTurned = IsTurned(input);
        double scale, scrollWidth, scrollHeight, windowWidth, windowHeight;
        double calculatedImageWidth, calculatedImageHeight;

        if (input.ScrollEnabled)
        {
            // Fit the image to the available width, but ignore the height constraint to allow for scrolling.
            scale = CalculateScale(isTurned, maxAvailableWidth, double.PositiveInfinity, imageWidth, imageHeight);

            calculatedImageWidth = imageWidth * scale;
            calculatedImageHeight = imageHeight * scale;

            scrollWidth = Math.Min(calculatedImageWidth, maxAvailableWidth);
            scrollHeight = Math.Min(calculatedImageHeight, maxAvailableHeight);

            if (isTurned)
            {
                windowWidth = Math.Max(scrollHeight + reserved.Bottom + reserved.Top + reserved.GalleryHeight,
                    SizeDefaults.WindowMinSize);
                windowHeight = Math.Max(scrollWidth + reserved.GalleryWidth, SizeDefaults.WindowMinSize);
            }
            else
            {
                windowWidth = Math.Max(scrollWidth + reserved.GalleryWidth, SizeDefaults.WindowMinSize);
                windowHeight = Math.Max(scrollHeight + reserved.Bottom + reserved.Top + reserved.GalleryHeight,
                    SizeDefaults.WindowMinSize);
            }
        }
        else
        {
            scale = CalculateScale(isTurned, maxAvailableWidth, maxAvailableHeight, imageWidth, imageHeight);

            calculatedImageWidth = imageWidth * scale;
            calculatedImageHeight = imageHeight * scale;

            scrollWidth = double.NaN;
            scrollHeight = double.NaN;

            if (isTurned)
            {
                windowWidth = Math.Max(calculatedImageHeight + reserved.GalleryWidth, SizeDefaults.WindowMinSize);
                windowHeight = Math.Max(calculatedImageWidth + reserved.Bottom + reserved.Top + reserved.GalleryHeight,
                    SizeDefaults.WindowMinSize);
            }
            else
            {
                windowWidth = Math.Max(calculatedImageWidth + reserved.GalleryWidth, SizeDefaults.WindowMinSize);
                windowHeight = Math.Max(calculatedImageHeight + reserved.Bottom + reserved.Top + reserved.GalleryHeight,
                    SizeDefaults.WindowMinSize);
            }
        }

        return new Fit(windowWidth, windowHeight, calculatedImageWidth, calculatedImageHeight,
            scrollWidth, scrollHeight, scale);
    }

    private static Fit FitPair(FitInput input)
    {
        var (width, height) = (input.ImageWidth, input.ImageHeight);
        var (secondaryWidth, secondaryHeight) = (input.SecondImageWidth, input.SecondImageHeight);
        if (width <= 0 || height <= 0)
        {
            return default;
        }

        var reserved = input.ReservedSpace;

        // Normalize the widths so both images share the largest height and scale evenly
        var largestHeight = Math.Max(height, secondaryHeight);
        var normalizedWidth = width * (largestHeight / height);
        var normalizedSecondaryWidth = secondaryWidth * (largestHeight / secondaryHeight);
        var combinedWidth = normalizedWidth + normalizedSecondaryWidth;

        double maxAvailableWidth, maxAvailableHeight;
        if (input.Mode is FitMode.AutoFit)
        {
            (maxAvailableWidth, maxAvailableHeight) = GetAvailableArea(input);
        }
        else
        {
            maxAvailableWidth = input.WindowContentWidth;
            maxAvailableHeight = input.WindowContentHeight;
        }

        var isTurned = IsTurned(input);
        double scale, calculatedCombinedWidth, calculatedLargestHeight;
        double scrollWidth, scrollHeight, windowWidth, windowHeight;

        if (input.ScrollEnabled)
        {
            scale = CalculateScale(isTurned, maxAvailableWidth, double.PositiveInfinity, combinedWidth, largestHeight);

            calculatedCombinedWidth = combinedWidth * scale;
            calculatedLargestHeight = largestHeight * scale;

            scrollWidth = Math.Min(calculatedCombinedWidth, maxAvailableWidth);
            scrollHeight = Math.Min(calculatedLargestHeight, maxAvailableHeight);

            windowWidth = scrollWidth + reserved.GalleryWidth;
            windowHeight = scrollHeight + reserved.Bottom + reserved.Top + reserved.GalleryHeight;
        }
        else
        {
            scale = CalculateScale(isTurned, maxAvailableWidth, maxAvailableHeight, combinedWidth, largestHeight);

            calculatedCombinedWidth = combinedWidth * scale;
            calculatedLargestHeight = largestHeight * scale;

            scrollWidth = double.NaN;
            scrollHeight = double.NaN;

            windowWidth = calculatedCombinedWidth + reserved.GalleryWidth;
            windowHeight = calculatedLargestHeight + reserved.Bottom + reserved.Top + reserved.GalleryHeight;
        }

        return new Fit(windowWidth, windowHeight, calculatedCombinedWidth, calculatedLargestHeight,
            scrollWidth, scrollHeight, scale);
    }

    private static bool IsTurned(FitInput input) => input.QuarterTurns % 2 != 0;

    private static double CalculateScale(bool isTurned, double maxWidth, double maxHeight, double width,
        double height) =>
        isTurned
            ? Math.Min(maxWidth / height, maxHeight / width)
            : Math.Min(maxWidth / width, maxHeight / height);

    private static (double Width, double Height) GetAvailableArea(FitInput input)
    {
        var reserved = input.ReservedSpace;
        switch (input.Mode)
        {
            case FitMode.Fullscreen:
                return (Math.Max(0, input.Screen.Width - reserved.GalleryWidth),
                    Math.Max(0, input.Screen.Height - reserved.GalleryHeight));
            case FitMode.FixedWindow:
                return (Math.Max(0, input.WindowContentWidth - reserved.GalleryWidth),
                    Math.Max(0, input.WindowContentHeight - reserved.GalleryHeight));
            default:
                var maxWindowWidth = input.Screen.WorkingAreaWidth - input.ScreenMargin;
                var maxWindowHeight = input.Screen.WorkingAreaHeight - input.ScreenMargin;

                // Never negative, even if the reserved space is larger than the screen
                return (Math.Max(0, maxWindowWidth - reserved.GalleryWidth),
                    Math.Max(0, maxWindowHeight - (reserved.GalleryHeight + reserved.Bottom + reserved.Top)));
        }
    }
}
