using System.Runtime.InteropServices;
using PicView.Core.Config;

namespace PicView.Core.Calculations
{
    public static class ImageSizeCalculationHelper
    {
        /// <summary>
        ///  Returns the interface size of the titlebar based on OS
        /// </summary>
        public static double GetInterfaceSize()
        {
            // TODO: find a more elegant solution
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
                return new ImageSize(0, 0, 0, 0, 0, 0, 0, 0);
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

                maxWidth = workAreaWidth - padding;
                maxHeight = height;
            }
            else if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                if (rotationAngle is 90 or 270)
                {
                    maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage
                        ? workAreaHeight - padding
                        : Math.Min(workAreaHeight - padding, height);
                    
                    maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage
                        ? workAreaWidth - padding
                        : Math.Min(workAreaWidth - padding, width);
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
                if (rotationAngle is 90 or 270)
                {
                    maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage
                        ? containerHeight - galleryHeight
                        : height;

                    maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage
                        ? containerHeight - galleryHeight
                        : Math.Min(containerHeight, height);
                }
                else
                {
                    maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage
                        ? containerWidth
                        : Math.Min(containerWidth, width);

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
            double scrollWidth, scrollHeight, xWidth, xHeight;
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                if (SettingsHelper.Settings.WindowProperties.AutoFit)
                {
                    xWidth = maxWidth - SizeDefaults.ScrollbarSize - 10;
                    xHeight = maxWidth * height / width;

                    scrollWidth = maxWidth;
                    scrollHeight = containerHeight - padding - 8;
                }
                else
                {
                    scrollWidth = containerWidth + SizeDefaults.ScrollbarSize;
                    scrollHeight = containerHeight;

                    xWidth = containerWidth - SizeDefaults.ScrollbarSize + 10;
                    xHeight = height / width * xWidth;
                }
            }
            else
            {
                scrollWidth = double.NaN;
                scrollHeight = double.NaN;

                xWidth = width * aspectRatio;
                xHeight = height * aspectRatio;
            }

            var titleMaxWidth = GetTitleMaxWidth(rotationAngle, xWidth, xHeight, monitorMinWidth, monitorMinHeight,
                interfaceSize, containerWidth);

            return new ImageSize(xWidth, xHeight, 0, scrollWidth, scrollHeight, titleMaxWidth, margin, aspectRatio);
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
                return new ImageSize(0, 0, 0, 0, 0, 0, 0, 0);
            }

            // Get sizes for both images
            var firstSize = GetImageSize(width, height, monitorWidth, monitorHeight, monitorMinWidth, monitorMinHeight,
                interfaceSize, rotationAngle, padding, dpiScaling, uiTopSize, uiBottomSize, galleryHeight,
                containerWidth,
                containerHeight);
            var secondSize = GetImageSize(secondaryWidth, secondaryHeight, monitorWidth, monitorHeight, monitorMinWidth,
                monitorMinHeight, interfaceSize, rotationAngle, padding, dpiScaling, uiTopSize, uiBottomSize,
                galleryHeight,
                containerWidth, containerHeight);

            // Determine maximum height for both images
            var xHeight = Math.Max(firstSize.Height, secondSize.Height);

            // Recalculate the widths to maintain the aspect ratio with the new maximum height
            var xWidth1 = firstSize.Width / firstSize.Height * xHeight;
            var xWidth2 = secondSize.Width / secondSize.Height * xHeight;

            // Combined width of both images
            var combinedWidth = xWidth1 + xWidth2;

            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                var widthPadding = SettingsHelper.Settings.ImageScaling.StretchImage ? 4 : padding;
                var availableWidth = monitorWidth - widthPadding;
                var availableHeight = monitorHeight - (widthPadding + uiBottomSize + uiTopSize);
                if (rotationAngle is 0 or 180)
                {
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
                    if (combinedWidth > availableHeight)
                    {
                        var scaleFactor = availableHeight / combinedWidth;
                        xWidth1 *= scaleFactor;
                        xWidth2 *= scaleFactor;
                        xHeight *= scaleFactor;
                        
                        combinedWidth = xWidth1 + xWidth2;
                    }
                }
            }
            else
            {
                if (rotationAngle is 0 or 180)
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
                else
                {
                    if (combinedWidth > containerHeight)
                    {
                        var scaleFactor = containerHeight / combinedWidth;
                        xWidth1 *= scaleFactor;
                        xWidth2 *= scaleFactor;
                        xHeight *= scaleFactor;
                        
                        combinedWidth = xWidth1 + xWidth2;
                    }
                }

            }

            double scrollWidth, scrollHeight;
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                if (SettingsHelper.Settings.WindowProperties.AutoFit)
                {
                    combinedWidth -= SizeDefaults.ScrollbarSize;
                    scrollWidth = combinedWidth + SizeDefaults.ScrollbarSize + 8;

                    var fullscreen = SettingsHelper.Settings.WindowProperties.Fullscreen ||
                                     SettingsHelper.Settings.WindowProperties.Maximized;
                    var borderSpaceHeight = fullscreen ? 0 : uiTopSize + uiBottomSize + galleryHeight;
                    var workAreaHeight = monitorHeight * dpiScaling - borderSpaceHeight;
                    scrollHeight = SettingsHelper.Settings.ImageScaling.StretchImage
                        ? workAreaHeight
                        : workAreaHeight - padding;
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

            var titleMaxWidth = GetTitleMaxWidth(rotationAngle, combinedWidth, xHeight, monitorMinWidth,
                monitorMinHeight, interfaceSize, containerWidth);

            var margin = firstSize.Height > secondSize.Height ? firstSize.Margin : secondSize.Margin;
            return new ImageSize(combinedWidth, xHeight, xWidth2, scrollWidth, scrollHeight, titleMaxWidth, margin,
                firstSize.AspectRatio);
        }


        public static double GetTitleMaxWidth(double rotationAngle, double width, double height, double monitorMinWidth,
            double monitorMinHeight, double interfaceSize, double containerWidth)
        {
            double titleMaxWidth;
            var maximized = SettingsHelper.Settings.WindowProperties.Fullscreen ||
                            SettingsHelper.Settings.WindowProperties.Maximized;

            if (SettingsHelper.Settings.WindowProperties.AutoFit && !maximized)
            {
                switch (rotationAngle)
                {
                    case 0 or 180:
                        titleMaxWidth = Math.Max(width, monitorMinWidth);
                        break;
                    case 90 or 270:
                        titleMaxWidth = Math.Max(height, monitorMinHeight);
                        break;
                    default:
                    {
                        var rotationRadians = rotationAngle * Math.PI / 180;
                        var newWidth = Math.Abs(width * Math.Cos(rotationRadians)) +
                                       Math.Abs(height * Math.Sin(rotationRadians));

                        titleMaxWidth = Math.Max(newWidth, monitorMinWidth);
                        break;
                    }
                }

                titleMaxWidth = titleMaxWidth - interfaceSize < interfaceSize
                    ? interfaceSize
                    : titleMaxWidth - interfaceSize;

                if (SettingsHelper.Settings.Zoom.ScrollEnabled)
                {
                    titleMaxWidth += SizeDefaults.ScrollbarSize + 4;
                }
            }
            else
            {
                // Fix title width to window size
                titleMaxWidth = containerWidth - interfaceSize <= 0 ? 0 : containerWidth - interfaceSize;
            }

            return titleMaxWidth;
        }

        public readonly struct ImageSize(
            double width,
            double height,
            double secondaryWidth,
            double scrollViewerWidth,
            double scrollViewerHeight,
            double titleMaxWidth,
            double margin,
            double aspectRatio)
        {
            public double TitleMaxWidth { get; } = titleMaxWidth;
            public double Width { get; } = width;
            public double Height { get; } = height;

            public double ScrollViewerWidth { get; } = scrollViewerWidth;
            public double ScrollViewerHeight { get; } = scrollViewerHeight;

            public double SecondaryWidth { get; } = secondaryWidth;
            public double Margin { get; } = margin;

            public double AspectRatio { get; } = aspectRatio;
        }
    }
}