﻿using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic.TransformImage;
using System.Windows;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.PicGallery.GalleryNavigation;
using static PicView.WPF.UILogic.ConfigureWindows;
using static PicView.WPF.UILogic.Sizing.WindowSizing;
using static PicView.WPF.UILogic.TransformImage.Rotation;

namespace PicView.WPF.UILogic.Sizing;

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
                    Fit();
                    return;
                }

                var pic = preloadValue.BitmapSource;
                if (pic != null)
                {
                    FitImage(pic.PixelWidth, pic.PixelHeight);
                }
            }
            else Fit();
        });
        return;
        void Fit()
        {
            if (GetMainWindow.MainImage.Source != null)
            {
                FitImage(GetMainWindow.MainImage.Source.Width, GetMainWindow.MainImage.Source.Height);
            }
            else if (XWidth > 0 && XHeight > 0)
            {
                FitImage(XWidth, XHeight);
            }
        }
    }

    /// <summary>
    /// Fits image size based on users screen resolution
    /// or window size
    /// </summary>
    /// <param name="width">The pixel width of the image</param>
    /// <param name="height">The pixel height of the image</param>
    internal static void FitImage(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        double maxWidth, maxHeight;
        var margin = 0d;
        var padding =
            MonitorInfo.DpiScaling <= 1
                ? 20 * MonitorInfo.DpiScaling
                : 0; // Padding to make it feel more comfortable
        var galleryHeight = 0d;

        if (UC.GetPicGallery is not null)
        {
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && UC.GetPicGallery.IsVisible)
            {
                galleryHeight = PicGalleryItemSize + ScrollbarSize;
            }
        }

        var borderSpaceHeight = SettingsHelper.Settings.WindowProperties.Fullscreen
            ? 0
            : GetMainWindow.LowerBar.ActualHeight + GetMainWindow.TitleBar.ActualHeight + galleryHeight;
        var borderSpaceWidth = SettingsHelper.Settings.WindowProperties.Fullscreen ? 0 : padding;

        var workAreaWidth = (MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - borderSpaceWidth;
        var workAreaHeight = (MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - borderSpaceHeight;

        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage ? workAreaWidth : Math.Min(workAreaWidth - padding, width);
            maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage ? workAreaHeight : Math.Min(workAreaHeight - padding, height);
        }
        else
        {
            maxWidth = SettingsHelper.Settings.ImageScaling.StretchImage
                ? GetMainWindow.ParentContainer.ActualWidth
                : Math.Min(GetMainWindow.ParentContainer.ActualWidth, width);
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage ? GetMainWindow.ParentContainer.ActualHeight : height;
            }
            else
            {
                maxHeight = SettingsHelper.Settings.ImageScaling.StretchImage
                    ? GetMainWindow.ParentContainer.ActualHeight - galleryHeight
                    : Math.Min(GetMainWindow.ParentContainer.ActualHeight - galleryHeight, height);
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
                if (UC.GetPicGallery is not null)
                {
                    if (PicGalleryItemSize is 0)
                    {
                        SetSize(SettingsHelper.Settings.Gallery.BottomGalleryItemSize);
                    }

                    margin = UC.GetPicGallery.IsVisible ? galleryHeight : 0;
                }
            }
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
                var newWidth = Math.Abs(width * Math.Cos(rotationRadians)) +
                               Math.Abs(height * Math.Sin(rotationRadians));
                var newHeight = Math.Abs(width * Math.Sin(rotationRadians)) +
                                Math.Abs(height * Math.Cos(rotationRadians));
                AspectRatio = Math.Min(maxWidth / newWidth, maxHeight / newHeight);
                break;
        }

        if (width * AspectRatio < 0 || height * AspectRatio < 0)
            return; // Fix weird error when entering fullscreen gallery

        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            GetMainWindow.MainImage.Height = maxWidth * height / width;
            GetMainWindow.MainImage.Width = maxWidth - ScrollbarSize;

            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                GetMainWindow.ParentContainer.Width = maxWidth - ScrollbarSize;
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
        }

        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && UC.GetPicGallery is not null)
        {
            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                UC.GetPicGallery.Width = SettingsHelper.Settings.Zoom.ScrollEnabled
                    ? GetMainWindow.ParentContainer.Width
                    : Math.Max(GetMainWindow.MinWidth, XWidth);
            }
        }

        // Update margin when from fullscreen gallery and when not
        GetMainWindow.Scroller.Margin = new Thickness(0, 0, 0, margin);

        if (ZoomLogic.IsZoomed)
        {
            ZoomLogic.ResetZoom(false);
        }

        if (SettingsHelper.Settings.WindowProperties.Fullscreen) return;

        // Update TitleBar maxWidth... Ugly code, but it works. Binding to ParentContainer.ActualWidth depends on correct timing.
        var interfaceSize =
            GetMainWindow.Logo.Width + GetMainWindow.GalleryButton.Width + GetMainWindow.RotateButton.Width +
            GetMainWindow.RotateButton.Width
            + GetMainWindow.MinButton.Width + GetMainWindow.FullscreenButton.Width +
            GetMainWindow.CloseButton.Width;

        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            // Vertically center or vertically and horizontally center
            CenterWindowOnScreen(GetMainWindow, SettingsHelper.Settings.WindowProperties.KeepCentered);

            var titleBarMaxWidth = RotationAngle is 0 or 180
                ? Math.Max(XWidth, GetMainWindow.MinWidth)
                : Math.Max(XHeight, GetMainWindow.MinHeight);

            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                GetMainWindow.TitleText.MaxWidth = titleBarMaxWidth;
            }
            else
            {
                GetMainWindow.TitleText.MaxWidth = titleBarMaxWidth - interfaceSize < interfaceSize
                    ? interfaceSize
                    : titleBarMaxWidth - interfaceSize;
            }
        }
        else
        {
            // Fix title width to window size
            GetMainWindow.TitleText.MaxWidth = GetMainWindow.ActualWidth - interfaceSize <= 0 ? 0 : GetMainWindow.ActualWidth - interfaceSize;
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