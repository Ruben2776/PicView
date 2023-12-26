using PicView.Core.Config;
using PicView.WPF.PicGallery;
using PicView.WPF.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.WPF.Animations;

internal static class AnimationHelper
{
    internal static readonly ColorAnimation ColorAnimation = new() { Duration = TimeSpan.FromSeconds(.35) };

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

    internal static Color GetCurrentColor(int colorTheme)
    {
        return colorTheme switch
        {
            2 => Color.FromRgb(230, 139, 238),
            3 => Color.FromRgb(219, 91, 61),
            4 => Color.FromRgb(32, 231, 107),
            5 => Color.FromRgb(249, 17, 16),
            6 => Color.FromRgb(68, 161, 160),
            7 => Color.FromRgb(54, 230, 204),
            8 => Color.FromRgb(254, 169, 85),
            9 => Color.FromRgb(151, 56, 235),
            10 => Color.FromRgb(27, 161, 226),
            11 => Color.FromRgb(255, 53, 197),
            12 => Color.FromRgb(34, 203, 151),
            _ => Color.FromRgb(26, 140, 240),
        };
    }

    internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colorTheme)
    {
        ColorAnimation.From = GetCurrentColor(colorTheme);
        ColorAnimation.To = Color.FromArgb(a, r, g, b);
        brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
    }

    internal static void MouseEnterColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colorTheme)
    {
        ColorAnimation.From = Color.FromArgb(a, r, g, b);
        ColorAnimation.To = GetCurrentColor(colorTheme);
        brush.BeginAnimation(SolidColorBrush.ColorProperty, ColorAnimation);
    }

    #endregion Color Events

    #region Color settings

    internal static Color GetPreferredColor()
    {
        return GetCurrentColor(SettingsHelper.Settings.Theme.ColorTheme);
    }

    #endregion Color settings

    #region Size Animations

    /// <summary>
    /// Animates the size of a gallery item.
    /// </summary>
    /// <param name="item">The gallery item to animate.</param>
    /// <param name="reduce">Indicates whether the animation is for unhovering the item.</param>
    /// <param name="fromSize">The initial size of the item.</param>
    /// <param name="toSize">The target size of the item.</param>
    internal static void SizeAnim(PicGalleryItem item, bool reduce, double fromSize, double toSize)
    {
        if (item.InnerBorder.Width > GalleryNavigation.PicGalleryItemSizeS && !reduce)
        {
            // Make sure it is not run consecutively
            return;
        }

        var animation = new DoubleAnimation
        {
            FillBehavior = FillBehavior.Stop,
            AccelerationRatio = 0.4,
            DecelerationRatio = 0.6
        };

        animation.Completed += delegate
        {
            item.InnerBorder.Width = toSize;
            item.InnerBorder.Height = toSize;
        };

        if (reduce)
        {
            animation.From = fromSize;
            animation.To = toSize;
            animation.Duration = TimeSpan.FromSeconds(.3);
        }
        else
        {
            animation.From = fromSize;
            animation.To = toSize;
            animation.Duration = TimeSpan.FromSeconds(.25);
        }

        item.InnerBorder.BeginAnimation(FrameworkElement.WidthProperty, animation);
        item.InnerBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);
    }

    /// <summary>
    /// Animates the height of an element.
    /// </summary>
    /// <param name="element">The element to animate.</param>
    /// <param name="fromHeight">The initial height of the element.</param>
    /// <param name="toHeight">The target height of the element.</param>
    /// <param name="reverse">Indicates whether the animation is reversed.</param>
    internal static void AnimateHeight(FrameworkElement element, double fromHeight, double toHeight, bool reverse)
    {
        var animation = new DoubleAnimation
        {
            FillBehavior = FillBehavior.Stop,
            AccelerationRatio = 0.4,
            DecelerationRatio = 0.6,
        };

        if (reverse)
        {
            animation.From = fromHeight;
            animation.To = toHeight;
            animation.Duration = TimeSpan.FromSeconds(.3);
        }
        else
        {
            animation.From = fromHeight;
            animation.To = toHeight;
            animation.Duration = TimeSpan.FromSeconds(.25);
        }

        animation.Completed += delegate { element.Height = toHeight; };

        element.BeginAnimation(FrameworkElement.HeightProperty, animation);
    }

    #endregion Size Animations
}