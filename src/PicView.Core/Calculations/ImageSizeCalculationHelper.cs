using PicView.Core.Config;
using System.Runtime.InteropServices;

namespace PicView.Core.Calculations;

public static class ImageSizeCalculationHelper
{
    public struct ImageSize(double width, double height, double titleMaxWidth, double margin)
    {
        public double TitleMaxWidth { get; private set; } = titleMaxWidth;
        public double Width { get; private set; } = width;
        public double Height { get; private set; } = height;

        public double Margin { get; private set; } = margin;
    }

    public static double GetInterfaceSize()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 165 : 228;
    }

    public static ImageSize GetImageSize(double width,
        double height,
        double monitorWidth,
        double monitorHeight,
        double monitorMinWidth,
        double monitorMinHeight,
        double interfaceSize,
        double rotationAngle,
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
            return new ImageSize(0, 0, 0, 0);
        }

        double aspectRatio;
        double maxWidth, maxHeight;
        var margin = 0d;

        var borderSpaceHeight = fullscreen ? 0 : uiTopSize + uiBottomSize + galleryHeight;
        var borderSpaceWidth = fullscreen ? 0 : padding;

        var workAreaWidth = monitorWidth * dpiScaling - borderSpaceWidth;
        var workAreaHeight = monitorHeight * dpiScaling - borderSpaceHeight;

        if (autoFit)
        {
            maxWidth = stretch ? workAreaWidth - padding : Math.Min(workAreaWidth - padding, width);
            maxHeight = stretch ? workAreaHeight - padding : Math.Min(workAreaHeight - padding, height);
        }
        else
        {
            maxWidth = stretch ? containerWidth : Math.Min(containerWidth, width);
            maxHeight = stretch
                ? containerHeight - galleryHeight
                : Math.Min(containerHeight - galleryHeight, height);
        }

        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            if (!SettingsHelper.Settings.UIProperties.ShowInterface && !SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI)
            {
                margin = 0;
            }
            else
            {
                margin = galleryHeight > 0 ? galleryHeight : 0;
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

        var xWidth = width * aspectRatio;
        var xHeight = height * aspectRatio;
        var titleMaxWidth = GetTitleMaxWidth(rotationAngle, xWidth, xHeight, monitorMinWidth, monitorMinHeight,
            monitorWidth, monitorHeight, interfaceSize, autoFit, containerWidth, scrollEnabled);

        return new ImageSize(xWidth, xHeight, titleMaxWidth, margin);
    }

    public static double GetTitleMaxWidth(double rotationAngle, double width, double height, double monitorMinWidth,
        double monitorMinHeight, double monitorWidth, double monitorHeight, double interfaceSize, bool autoFit,
        double containerWidth, bool scrollEnabled)
    {
        double titleMaxWidth;
        if (autoFit)
        {
            titleMaxWidth = rotationAngle is 0 or 180
                ? Math.Max(width, monitorMinWidth)
                : Math.Max(height, monitorMinHeight);

            if (scrollEnabled)
            {
                return titleMaxWidth;
            }

            titleMaxWidth = titleMaxWidth - interfaceSize < interfaceSize
                ? interfaceSize
                : titleMaxWidth - interfaceSize;
        }
        else
        {
            // Fix title width to window size
            titleMaxWidth = containerWidth - interfaceSize <= 0 ? 0 : containerWidth - interfaceSize;
        }

        return titleMaxWidth;
    }
}