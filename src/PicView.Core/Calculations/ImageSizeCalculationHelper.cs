using System.Runtime.InteropServices;
using PicView.Core.Config;

namespace PicView.Core.Calculations
{
    public static class ImageSizeCalculationHelper
    {
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
                return new ImageSize(0, 0, 0, 0, 0, 0);
            }

            double aspectRatio;
            double maxWidth, maxHeight;
            var margin = 0d;

            var fullscreen = SettingsHelper.Settings.WindowProperties.Fullscreen ||
                             SettingsHelper.Settings.WindowProperties.Maximized;

            var borderSpaceHeight = fullscreen ? 0 : uiTopSize + uiBottomSize + galleryHeight;
            var borderSpaceWidth = fullscreen ? 0 : padding;

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
                    maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage
                        ? workAreaWidth - padding
                        : Math.Min(workAreaWidth - padding, width);
                    maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage
                        ? workAreaHeight - padding
                        : Math.Min(workAreaHeight - padding, height);
                }
            }
            else
            {
                maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage
                    ? containerWidth
                    : Math.Min(containerWidth, width);

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
                if (!SettingsHelper.Settings.UIProperties.ShowInterface)
                {
                    if (SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI)
                    {
                        margin = galleryHeight > 0 ? galleryHeight : 0;
                    }
                    else
                    {
                        margin = 0;
                    }
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

            var titleMaxWidth = GetTitleMaxWidth(rotationAngle, xWidth, xHeight, monitorMinWidth, monitorMinHeight,
                interfaceSize, containerWidth);

            return new ImageSize(xWidth, xHeight, 0, titleMaxWidth, margin, aspectRatio);
        }

        public static ImageSize GetImageSize(double width,
            double height,
            double secondaryWidth,
            double secondaryHeight,
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
            if (width <= 0 || height <= 0 || secondaryWidth <= 0 || secondaryHeight <= 0 || rotationAngle > 360 ||
                rotationAngle < 0)
            {
                return new ImageSize(0, 0, 0, 0, 0,0);
            }

            // Get sizes for both images
            var firstSize = GetImageSize(width, height, monitorWidth, monitorHeight, monitorMinWidth, monitorMinHeight,
                interfaceSize, rotationAngle, padding, dpiScaling, uiTopSize, uiBottomSize, galleryHeight, containerWidth,
                containerHeight);
            var secondSize = GetImageSize(secondaryWidth, secondaryHeight, monitorWidth, monitorHeight, monitorMinWidth,
                monitorMinHeight, interfaceSize, rotationAngle, padding, dpiScaling, uiTopSize, uiBottomSize, galleryHeight,
                containerWidth, containerHeight);
    
            // Determine maximum height for both images
            var xHeight = Math.Max(firstSize.Height, secondSize.Height);

            // Recalculate the widths to maintain the aspect ratio with the new maximum height
            var xWidth1 = (firstSize.Width / firstSize.Height) * xHeight;
            var xWidth2 = (secondSize.Width / secondSize.Height) * xHeight;

            // Combined width of both images
            var combinedWidth = xWidth1 + xWidth2;

            var widthPadding = SettingsHelper.Settings.ImageScaling.StretchImage ? 4 : padding;
            var availableWidth = monitorWidth - widthPadding;

            // If combined width exceeds available width, scale both images down proportionally
            if (combinedWidth > availableWidth)
            {
                var scaleFactor = availableWidth / combinedWidth;
                xWidth1 *= scaleFactor;
                xWidth2 *= scaleFactor;
                xHeight *= scaleFactor;
                
                combinedWidth = xWidth1 + xWidth2;
            }

            var titleMaxWidth = GetTitleMaxWidth(rotationAngle, combinedWidth, xHeight, monitorMinWidth, monitorMinHeight, interfaceSize, containerWidth);

            var margin = firstSize.Height > secondSize.Height ? firstSize.Margin : secondSize.Margin;
            return new ImageSize(combinedWidth, xHeight, xWidth2, titleMaxWidth, margin, firstSize.AspectRatio);
        }


        public static double GetTitleMaxWidth(double rotationAngle, double width, double height, double monitorMinWidth,
            double monitorMinHeight, double interfaceSize, double containerWidth)
        {
            double titleMaxWidth;
            var maximized = SettingsHelper.Settings.WindowProperties.Fullscreen ||
                            SettingsHelper.Settings.WindowProperties.Maximized;

            if (SettingsHelper.Settings.WindowProperties.AutoFit && !maximized)
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

        public struct ImageSize(double width, double height, double secondaryWidth, double titleMaxWidth, double margin, double aspectRatio)
        {
            public double TitleMaxWidth { get; private set; } = titleMaxWidth;
            public double Width { get; } = width;
            public double Height { get; } = height;
            
            public double SecondaryWidth { get; } = secondaryWidth;
            public double Margin { get; private set; } = margin;

            public double AspectRatio { get; } = aspectRatio;
        }
    }
}