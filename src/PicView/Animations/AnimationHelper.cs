using PicView.ChangeImage;
using PicView.PicGallery;
using PicView.Properties;
using PicView.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.Animations;

internal static class AnimationHelper
{
    private static readonly ColorAnimation ColorAnimation = new() { Duration = TimeSpan.FromSeconds(.35) };

    #region Fade

    internal static void Fade(UIElement element, double to, Duration duration)
    {
        var da = new DoubleAnimation
        {
            To = to,
            Duration = duration
        };

        try
        {
            element.BeginAnimation(UIElement.OpacityProperty, da);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    internal static bool Fade(UIElement element, Duration duration, TimeSpan beginTime, double from, double to)
    {
        var da = new DoubleAnimation
        {
            From = from,
            To = to,
            BeginTime = beginTime,
            Duration = duration
        };

        try
        {
            element.BeginAnimation(UIElement.OpacityProperty, da);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }

    #endregion Fade

    #region Color Events

    internal static void MouseEnterBgTexColor(Brush brush)
    {
        ColorAnimation.From = Color.FromArgb(0, 0, 0, 0);
        ColorAnimation.To = (Color)Application.Current.Resources["BackgroundHoverHighlight"];
        brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
    }

    internal static void MouseLeaveBgTexColor(Brush brush)
    {
        ColorAnimation.From = (Color)Application.Current.Resources["BackgroundHoverHighlight"];
        ColorAnimation.To = Color.FromArgb(0, 0, 0, 0);
        brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
    }

    internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush)
    {
        ColorAnimation.From = GetPreferredColor();
        ColorAnimation.To = Color.FromArgb(a, r, g, b);
        brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
    }

    internal static void MouseOverColorEvent(byte a, byte r, byte g, byte b, Brush brush)
    {
        ColorAnimation.From = Color.FromArgb(a, r, g, b);
        ColorAnimation.To = GetPreferredColor();
        brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
    }

    internal static void LightThemeMouseEvent(UIElement uIElement, Brush brush)
    {
        var c = (Color)Application.Current.Resources["IconColor"];

        uIElement.MouseEnter += delegate
        {
            ColorAnimation.From = Color.FromRgb(c.R, c.G, c.B);
            ColorAnimation.To = Colors.White;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
        };

        uIElement.MouseLeave += delegate
        {
            ColorAnimation.From = Colors.White;
            ColorAnimation.To = Color.FromRgb(c.R, c.G, c.B);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
        };
    }

    internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colorTheme)
    {
        ColorAnimation.From = colorTheme switch
        {
            2 => Settings.Default.Pink,
            3 => Settings.Default.Orange,
            4 => Settings.Default.Green,
            5 => Settings.Default.Red,
            6 => Settings.Default.Teal,
            7 => Settings.Default.Aqua,
            8 => Settings.Default.Golden,
            9 => Settings.Default.Purple,
            10 => Settings.Default.Cyan,
            11 => Settings.Default.Magenta,
            12 => Settings.Default.Lime,
            _ => Settings.Default.Blue,
        };
        ColorAnimation.To = Color.FromArgb(a, r, g, b);
        brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
    }

    internal static void MouseEnterColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colorTheme)
    {
        ColorAnimation.From = Color.FromArgb(a, r, g, b);
        ColorAnimation.To = colorTheme switch
        {
            2 => Settings.Default.Pink,
            3 => Settings.Default.Orange,
            4 => Settings.Default.Green,
            5 => Settings.Default.Red,
            6 => Settings.Default.Teal,
            7 => Settings.Default.Aqua,
            8 => Settings.Default.Golden,
            9 => Settings.Default.Purple,
            10 => Settings.Default.Cyan,
            11 => Settings.Default.Magenta,
            12 => Settings.Default.Lime,
            _ => Settings.Default.Blue,
        };
        brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
    }

    #endregion Color Events

    #region Color settings

    internal static Color GetPreferredColor()
    {
        return Settings.Default.ColorTheme switch
        {
            2 => Settings.Default.Pink,
            3 => Settings.Default.Orange,
            4 => Settings.Default.Green,
            5 => Settings.Default.Red,
            6 => Settings.Default.Teal,
            7 => Settings.Default.Aqua,
            8 => Settings.Default.Golden,
            9 => Settings.Default.Purple,
            10 => Settings.Default.Cyan,
            11 => Settings.Default.Magenta,
            12 => Settings.Default.Lime,
            _ => Settings.Default.Blue,
        };
    }

    #endregion Color settings

    #region Size Animation

    internal static void HoverSizeAnim(PicGalleryItem item, bool unHover, double from, double to)
    {
        if (item.Id == Navigation.FolderIndex || item.Id == GalleryNavigation.SelectedGalleryItem || Settings.Default.FullscreenGallery)
        {
            return;
        }
        if (item.InnerBorder.Width > GalleryNavigation.PicGalleryItemSizeS && unHover == false)
        {
            // Make sure it is not run consecutively
            return;
        }

        var da = new DoubleAnimation
        {
            FillBehavior = FillBehavior.Stop,
            AccelerationRatio = 0.4,
            DecelerationRatio = 0.6
        };
        da.Completed += delegate
        {
            item.InnerBorder.Width = to;
            item.InnerBorder.Height = to;
        };

        if (unHover)
        {
            da.From = from;
            da.To = to;
            da.Duration = TimeSpan.FromSeconds(.3);
        }
        else
        {
            da.From = from;
            da.To = to;
            da.Duration = TimeSpan.FromSeconds(.25);
        }

        item.InnerBorder.BeginAnimation(FrameworkElement.WidthProperty, da);
        item.InnerBorder.BeginAnimation(FrameworkElement.HeightProperty, da);
    }

    internal static void HeightAnimation(FrameworkElement element, double from, double to, bool reverse)
    {
        var da = new DoubleAnimation
        {
            FillBehavior = FillBehavior.Stop,
            AccelerationRatio = 0.4,
            DecelerationRatio = 0.6,
        };

        if (reverse)
        {
            da.From = from;
            da.To = to;
            da.Duration = TimeSpan.FromSeconds(.3);
        }
        else
        {
            da.From = from;
            da.To = to;
            da.Duration = TimeSpan.FromSeconds(.25);
        }

        da.Completed += delegate
        {
            element.Height = to;
        };

        element.BeginAnimation(FrameworkElement.HeightProperty, da);
    }

    #endregion Size Animation
}