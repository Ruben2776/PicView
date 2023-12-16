namespace PicView.Core.Calculations;

/// <summary>
/// Provides methods for calculating image size based on monitor dimensions, rotation angle, and stretch parameter.
/// </summary>
public static class ImageSizeCalculationHelper
{
    /// <summary>
    /// Represents the dimensions of an image.
    /// </summary>
    public struct ImageSize
    {
        /// <summary>
        /// Gets the width of the image.
        /// </summary>
        public double Width { get; private set; }

        /// <summary>
        /// Gets the height of the image.
        /// </summary>
        public double Height { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSize"/> struct with the specified width and height.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        public ImageSize(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Calculates the adjusted image size based on specified parameters.
    /// </summary>
    /// <param name="width">The original width of the image.</param>
    /// <param name="height">The original height of the image.</param>
    /// <param name="monitorWidth">The width of the monitor.</param>
    /// <param name="monitorHeight">The height of the monitor.</param>
    /// <param name="rotationAngle">The rotation angle of the image in degrees.</param>
    /// <param name="stretch">A boolean indicating whether to stretch the image to fit the container.</param>
    /// <param name="padding">The padding applied to the image.</param>
    /// <param name="dpiScaling">The DPI scaling factor of the monitor.</param>
    /// <param name="fullscreen">A boolean indicating whether the image is displayed in fullscreen mode.</param>
    /// <param name="uiTopSize">The size of the top UI element.</param>
    /// <param name="uiBottomSize">The size of the bottom UI element.</param>
    /// <param name="galleryHeight">The height of the gallery.</param>
    /// <param name="autoFit">A boolean indicating whether to automatically fit the image within the monitor.</param>
    /// <param name="containerWidth">The width of the container.</param>
    /// <param name="containerHeight">The height of the container.</param>
    /// <param name="scrollEnabled">A boolean indicating whether scrolling is enabled.</param>
    /// <returns>An <see cref="ImageSize"/> struct representing the adjusted image size.</returns>
    public static ImageSize GetImageSize(double width,
        double height,
        double monitorWidth,
        double monitorHeight,
        int rotationAngle,
        bool stretch,
        double padding,
        double dpiScaling,
        bool fullscreen,
        double uiTopSize,
        double uiBottomSize,
        double galleryHeight,
        bool autoFit,
        double containerWidth,
        double containerHeight,
        bool scrollEnabled)
    {
        if (width <= 0 || height <= 0 || rotationAngle > 360 || rotationAngle < 0)
        {
            // Invalid input, return zero-sized image
            return new ImageSize(0, 0);
        }

        double aspectRatio;
        double maxWidth, maxHeight;

        var borderSpaceHeight = fullscreen ? 0 : uiTopSize + uiBottomSize + galleryHeight;
        var borderSpaceWidth = fullscreen ? 0 : padding;

        var workAreaWidth = (monitorWidth * dpiScaling) - borderSpaceWidth;
        var workAreaHeight = (monitorHeight * dpiScaling) - borderSpaceHeight;

        if (autoFit)
        {
            maxWidth = stretch ? workAreaWidth : Math.Min(workAreaWidth - padding, width);
            maxHeight = stretch ? workAreaHeight : Math.Min(workAreaHeight - padding, height);
        }
        else
        {
            maxWidth = stretch ? containerWidth : Math.Min(containerWidth, width);
            if (scrollEnabled)
            {
                maxHeight = stretch ? containerHeight : height;
            }
            else
            {
                maxHeight = stretch
                    ? containerHeight - galleryHeight
                    : Math.Min(containerHeight - galleryHeight, height);
            }
        }

        // aspect ratio calculation
        switch (rotationAngle)
        {
            case 0:
            case 180:
                aspectRatio = Math.Min(maxWidth / width, maxHeight / height);
                break;

            case 90:
            case 270:
                aspectRatio = Math.Min(maxWidth / height, maxHeight / width);
                break;

            default:
                var rotationRadians = rotationAngle * Math.PI / 180;
                var newWidth = Math.Abs(width * Math.Cos(rotationRadians)) +
                               Math.Abs(height * Math.Sin(rotationRadians));
                var newHeight = Math.Abs(width * Math.Sin(rotationRadians)) +
                                Math.Abs(height * Math.Cos(rotationRadians));
                aspectRatio = Math.Min(maxWidth / newWidth, maxHeight / newHeight);
                break;
        }

        return new ImageSize(width * aspectRatio, height * aspectRatio);
    }
}