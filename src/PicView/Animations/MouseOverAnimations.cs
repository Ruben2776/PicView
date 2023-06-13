using PicView.Properties;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.Animations;

internal static class MouseOverAnimations
{
    /*

        Adds MouseOver events for the given elements with the AnimationHelper.
        Changes color depending on the users settings.

    */

    private static readonly Color IconColor = (Color)Application.Current.Resources["IconColor"];

    private static readonly Color BackgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorAlt"];

    internal static void SetButtonIconMouseOverAnimations(UIElement uIElement, Brush backgroundBrush, Brush iconBrush, bool force = false)
    {
        if (Settings.Default.DarkTheme || force)
        {
            uIElement.MouseLeftButtonDown += delegate
            {
                ButtonMouseOverAnim(iconBrush);
                ButtonMouseOverAnim(backgroundBrush);
                AnimationHelper.MouseEnterBgTexColor(backgroundBrush);
            };

            uIElement.MouseEnter += delegate
            {
                ButtonMouseOverAnim(iconBrush);
                AnimationHelper.MouseEnterBgTexColor(backgroundBrush);
            };

            uIElement.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(iconBrush);
                AnimationHelper.MouseLeaveBgTexColor(backgroundBrush);
            };
        }
        else
        {
            uIElement.MouseEnter += (_, _) => ButtonMouseOverAnim(backgroundBrush, true);
            uIElement.MouseLeave += (_, _) => ButtonMouseLeaveAnimBgColor(backgroundBrush);
            AnimationHelper.LightThemeMouseEvent(uIElement, iconBrush);
        }
    }

    #region ALtInterface hover anims

    private static readonly ColorAnimation CcAnim = new() { Duration = TimeSpan.FromSeconds(.32) };
    private static readonly ColorAnimation CcAnim2 = new() { Duration = TimeSpan.FromSeconds(.2) };
    private static readonly SolidColorBrush BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"];

    internal static void AltInterfaceMouseOver(Brush foreground, Brush background, Brush border)
    {
        CcAnim.From = (Color)Application.Current.Resources["IconColor"];
        CcAnim.To = AnimationHelper.GetPreferredColor();

        foreground.BeginAnimation(SolidColorBrush.ColorProperty, CcAnim);

        CcAnim2.From = (Color)Application.Current.Resources["AltInterface"];
        CcAnim2.To = (Color)Application.Current.Resources["AltInterfaceW"];

        background.BeginAnimation(SolidColorBrush.ColorProperty, CcAnim2);
        AnimationHelper.MouseOverColorEvent(
            BorderBrush.Color.A,
            BorderBrush.Color.R,
            BorderBrush.Color.G,
            BorderBrush.Color.B,
            border);
    }

    internal static void AltInterfaceMouseLeave(Brush foreground, Brush background, Brush border)
    {
        CcAnim.From = AnimationHelper.GetPreferredColor();
        CcAnim.To = (Color)Application.Current.Resources["IconColor"];

        foreground.BeginAnimation(SolidColorBrush.ColorProperty, CcAnim);

        CcAnim2.From = (Color)Application.Current.Resources["AltInterfaceW"];
        CcAnim2.To = (Color)Application.Current.Resources["AltInterface"];

        background.BeginAnimation(SolidColorBrush.ColorProperty, CcAnim2);
        AnimationHelper.MouseLeaveColorEvent(
            BorderBrush.Color.A,
            BorderBrush.Color.R,
            BorderBrush.Color.G,
            BorderBrush.Color.B,
            border);
    }

    #endregion ALtInterface hover anims

    #region 1x

    internal static void ButtonMouseOverAnim(Brush brush, bool transparent = false)
    {
        if (transparent)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, brush);
        }
        else
        {
            AnimationHelper.MouseOverColorEvent(
                IconColor.A,
                IconColor.R,
                IconColor.G,
                IconColor.B,
                brush
            );
        }
    }

    internal static void ButtonMouseLeaveAnim(Brush brush, bool transparent = false)
    {
        if (transparent)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, brush);
        }
        else
        {
            AnimationHelper.MouseLeaveColorEvent(
                IconColor.A,
                IconColor.R,
                IconColor.G,
                IconColor.B,
                brush
            );
        }
    }

    internal static void ButtonMouseLeaveAnimBgColor(Brush brush)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            brush
        );
    }

    #endregion 1x
}