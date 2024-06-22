using PicView.Core.Config;
using System.Runtime.InteropServices;

namespace PicView.Core.Calculations;

public static class ImageSizeCalculationHelper
{
    
    public struct ImageSize(double width, double height, double titleMaxWidth, double margin, double aspectRatio)
    {
        public double TitleMaxWidth { get; private set; } = titleMaxWidth;
        public double Width { get; private set; } = width;
        public double Height { get; private set; } = height;
        public double Margin { get; private set; } = margin;
        
        public double AspectRatio { get; private set; } = aspectRatio;
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
        double padding,
        double dpiScaling,
        double uiTopSize,
        double uiBottomSize,
        double galleryHeight,
        double containerWidth,
        double containerHeight)
    {
        if (width <= 0 || height <= 0 || rotationAngle > 360 || rotationAngle < 0)
        {
            return new ImageSize(0, 0, 0, 0,0);
        }

        double aspectRatio;
        double maxWidth, maxHeight;
        var margin = 0d;

        var borderSpaceHeight = SettingsHelper.Settings.WindowProperties.Fullscreen ?
            0 : uiTopSize + uiBottomSize + galleryHeight;
        var borderSpaceWidth = SettingsHelper.Settings.WindowProperties.Fullscreen ?
            0 : padding;

        var workAreaWidth = monitorWidth * dpiScaling - borderSpaceWidth;
        var workAreaHeight = monitorHeight * dpiScaling - borderSpaceHeight;
        
        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            workAreaWidth -= SizeDefaults.ScrollbarSize * dpiScaling;
            containerWidth -= SizeDefaults.ScrollbarSize * dpiScaling;
        }

        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                maxWidth = workAreaWidth - padding;
                maxHeight = workAreaHeight - padding;
            }
            else
            {
                maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage ? workAreaWidth - padding : Math.Min(workAreaWidth - padding, width);
                maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage ? workAreaHeight - padding : Math.Min(workAreaHeight - padding, height);
            }
        }
        else
        {
            maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage ? containerWidth : Math.Min(containerWidth, width);
            
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage
                    ? Math.Max(containerHeight, height)
                    : height;
            }
            else
            {
                maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage
                    ? containerHeight - galleryHeight
                    : Math.Min(containerHeight - galleryHeight, height);
            }
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

        // Fit image by aspect ratio calculation
        // and update values
        var xWidth = width * aspectRatio;
        var xHeight = height * aspectRatio;
        
        var titleMaxWidth = GetTitleMaxWidth(rotationAngle, xWidth, xHeight, monitorMinWidth, monitorMinHeight, interfaceSize, containerWidth);

        return new ImageSize(xWidth, xHeight, titleMaxWidth, margin, aspectRatio);
    }

    public static double GetTitleMaxWidth(double rotationAngle, double width, double height, double monitorMinWidth,
        double monitorMinHeight, double interfaceSize, double containerWidth)
    {
        double titleMaxWidth;
        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            titleMaxWidth = rotationAngle is 0 or 180
                ? Math.Max(width, monitorMinWidth)
                : Math.Max(height, monitorMinHeight);

            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
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