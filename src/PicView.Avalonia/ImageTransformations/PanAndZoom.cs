using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Avalonia.Helpers;
using PicView.Core.Config;

namespace PicView.Avalonia.ImageTransformations;

public static class PanAndZoom
{
    public static ScaleTransform? ScaleTransform;
    public static TranslateTransform? TranslateTransform;

    private static Border? _border;
    private static Image? _image;
    private static ScrollViewer? _scrollViewer;
    private static Control? _parentContainer;

    private static Point _origin;
    private static Point _start;

    public static double ZoomValue { get; private set; }
    
    public static string ZoomPercentage
    {
        get
        {
            if (ScaleTransform == null || ZoomValue is 1)
            {
                return string.Empty;
            }

            var zoom = Math.Round(ZoomValue * 100);

            return zoom + "%";
        }
    }
    
    public static bool IsZoomed
    {
        get
        {
            if (ScaleTransform is null)
            {
                return false;
            }

            return ZoomValue is not 1;
        }
    }
    
    public static void TriggerScalingModeUpdate()
    {
        // TODO: Implement later
    }
    
    public static void InitializeZoom(Border border, Image image, ScrollViewer scrollViewer, Control parentContainer)
    {
        _border = border;
        _image = image;
        _scrollViewer = scrollViewer;
        _parentContainer = parentContainer;
        
        // Initialize transforms
        border.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(),
                new TranslateTransform()
            }
        };
        
        parentContainer.ClipToBounds = true;
        border.ClipToBounds = true;

        // Set transforms to UI elements
        ScaleTransform = (ScaleTransform)((TransformGroup)
                border.RenderTransform)
            .Children.First(tr => tr is ScaleTransform);

        TranslateTransform = (TranslateTransform)((TransformGroup)
                border.RenderTransform)
            .Children.First(tr => tr is TranslateTransform);
    }

    #region Pan
    
    public static void PreparePanImage(PointerPressedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        if (!desktop.MainWindow.IsActive || !desktop.MainWindow.IsVisible || ScaleTransform is null)
        {
            return;
        }

        // Report position for image drag
        e.Pointer.Capture(desktop.MainWindow);
        _start = e.GetPosition(_parentContainer);
        _origin = new Point(TranslateTransform.X, TranslateTransform.Y);
    }
    
    public static void PanImage(PointerEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        if (!desktop.MainWindow.IsVisible || ScaleTransform is null)
        {
            return;
        }
        if (!desktop.MainWindow.IsPointerOver)
        {
            return;
        }

        // Drag image by modifying X,Y coordinates
        var dragMousePosition = _start - e.GetPosition(desktop.MainWindow);

        var newXproperty = _origin.X - dragMousePosition.X;
        var newYproperty = _origin.Y - dragMousePosition.Y;

        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            TranslateTransform.X = newXproperty;
            TranslateTransform.Y = newYproperty;
            e.Handled = true;
            return;
        }

        // Keep panning it in bounds

        var actualScrollWidth = _scrollViewer.Bounds.Width;
        var actualBorderWidth = _border.Bounds.Width;
        var actualScrollHeight = _scrollViewer.Bounds.Height;
        var actualBorderHeight = _border.Bounds.Height;

        var isXOutOfBorder = actualScrollWidth < actualBorderWidth * ScaleTransform.ScaleX;
        var isYOutOfBorder = actualScrollHeight < actualBorderHeight * ScaleTransform.ScaleY;
        var maxX = actualScrollWidth - actualBorderWidth * ScaleTransform.ScaleX;
        var maxY = actualScrollHeight - actualBorderHeight * ScaleTransform.ScaleY;

        if (isXOutOfBorder && newXproperty < maxX || isXOutOfBorder == false && newXproperty > maxX)
        {
            newXproperty = maxX;
        }

        if (isXOutOfBorder && newYproperty < maxY || isYOutOfBorder == false && newYproperty > maxY)
        {
            newYproperty = maxY;
        }

        if (isXOutOfBorder && newXproperty > 0 || isXOutOfBorder == false && newXproperty < 0)
        {
            newXproperty = 0;
        }

        if (isYOutOfBorder && newYproperty > 0 || isYOutOfBorder == false && newYproperty < 0)
        {
            newYproperty = 0;
        }

        TranslateTransform.X = newXproperty;
        TranslateTransform.Y = newYproperty;

        e.Handled = true;
    }
    
    #endregion

    #region Zoom
    
    public static void ResetZoom(bool animate = true)
    {
        if (_image == null || ScaleTransform == null || TranslateTransform == null)
        {
            return;
        }

        ZoomValue = 1;

        if (animate)
        {
            BeginZoomAnimation(1);
        }
        else
        {
            ScaleTransform.ScaleX = ScaleTransform.ScaleY = 1.0;
            TranslateTransform.X = TranslateTransform.Y = 0.0;
        }
    }
    
    public static void Zoom(bool isZoomIn)
    {
        var currentZoom = ScaleTransform.ScaleX;
        var zoomSpeed = SettingsHelper.Settings.Zoom.ZoomSpeed;

        switch (currentZoom)
        {
            // Increase speed based on the current zoom level
            case > 14 when isZoomIn:
                return;

            case > 4:
                zoomSpeed += 1.5;
                break;

            case > 3.2:
                zoomSpeed += 1;
                break;

            case > 1.6:
                zoomSpeed += 0.5;
                break;
        }

        if (!isZoomIn)
        {
            zoomSpeed = -zoomSpeed;
        }

        currentZoom += zoomSpeed;
        currentZoom = Math.Max(0.09, currentZoom);
        if (SettingsHelper.Settings.Zoom.AvoidZoomingOut && currentZoom < 1.0)
        {
            ResetZoom();
        }
        else
        {
            ZoomToValue(currentZoom);
        }
    }
    
    private static void ZoomToValue(double value)
    {
        if (_image.Source is null)
            return;

        ZoomValue = value;
        TriggerScalingModeUpdate();

        BeginZoomAnimation(ZoomValue);
    }
    
    private static async Task BeginZoomAnimation(double zoomValue)
    {
        var point = _border.PointToScreen(new Point());

        // Calculate new position
        var absoluteX = point.X * ScaleTransform.ScaleX + TranslateTransform.X;
        var absoluteY = point.Y * ScaleTransform.ScaleY + TranslateTransform.Y;

        // Reset to zero if value is one, which is reset
        var newTranslateValueX = Math.Abs(zoomValue - 1) > .1 ? absoluteX - point.X * zoomValue : 0;
        var newTranslateValueY = Math.Abs(zoomValue - 1) > .1 ? absoluteY - point.Y * zoomValue : 0;

        var zoomAnim = AnimationsHelper.ZoomAnimation(ScaleTransform.ScaleX, 
                                                                zoomValue,
                                                                absoluteX, 
                                                                absoluteY, 
                                                                newTranslateValueX, 
                                                                newTranslateValueY,
                                                                TimeSpan.FromSeconds(25));

        // Start animations
        //await zoomAnim.RunAsync(_image);

        // Set new values
        ScaleTransform.ScaleX = zoomValue;
        ScaleTransform.ScaleY = zoomValue;
        TranslateTransform.X = newTranslateValueX;
        TranslateTransform.Y = newTranslateValueY;
        ZoomValue = zoomValue;
    }
    
    #endregion
}
