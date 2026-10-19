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

        // Fit the image as it is shown on screen, after its quarter turns
        var isTurned = input.QuarterTurns % 2 != 0;
        var shownWidth = isTurned ? imageHeight : imageWidth;
        var shownHeight = isTurned ? imageWidth : imageHeight;

        var (maxAvailableWidth, maxAvailableHeight) = GetAvailableArea(input);
        if (!input.ZoomToFit)
        {
            // Never scale the image beyond 100%
            maxAvailableWidth = Math.Min(maxAvailableWidth, shownWidth);
            maxAvailableHeight = Math.Min(maxAvailableHeight, shownHeight);
        }

        double scale, viewWidth, viewHeight, scrollWidth, scrollHeight;
        if (input.ScrollEnabled)
        {
            // Fit the available width and scroll the rest of the height
            scale = maxAvailableWidth / shownWidth;
            scrollWidth = viewWidth = Math.Min(shownWidth * scale, maxAvailableWidth);
            scrollHeight = viewHeight = Math.Min(shownHeight * scale, maxAvailableHeight);
        }
        else
        {
            scale = Math.Min(maxAvailableWidth / shownWidth, maxAvailableHeight / shownHeight);
            viewWidth = shownWidth * scale;
            viewHeight = shownHeight * scale;
            scrollWidth = scrollHeight = double.NaN;
        }

        var reserved = input.ReservedSpace;
        var windowWidth = Math.Max(viewWidth + reserved.GalleryWidth, SizeDefaults.WindowMinSize);
        var windowHeight = Math.Max(viewHeight + reserved.Top + reserved.Bottom + reserved.GalleryHeight,
            SizeDefaults.WindowMinSize);

        return new Fit(windowWidth, windowHeight, imageWidth * scale, imageHeight * scale,
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

        var isTurned = input.QuarterTurns % 2 != 0;
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
