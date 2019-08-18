using PicView.Properties;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PicView
{
    internal static class AnimationHelper
    {
        //private static DoubleAnimation da = new DoubleAnimation();
        private static readonly ColorAnimation ccAnim = new ColorAnimation { Duration = TimeSpan.FromSeconds(.35) };

        #region Fade
        internal static void Fade(UIElement element, double to, Duration duration)
        {
            var da = new DoubleAnimation()
            {
                To = to,
                Duration = duration
            };
            element.BeginAnimation(UIElement.OpacityProperty, da);
        }

        internal static void Fade(UIElement element, Duration duration, TimeSpan begintime, double from, double to)
        {
            var da = new DoubleAnimation()
            {
                From = from,
                To = to,
                BeginTime = begintime,
                Duration = duration
            };
            element.BeginAnimation(UIElement.OpacityProperty, da);
        }

        internal static void FadeWindow(Window window, Double to, Duration duration)
        {
            var anim = new DoubleAnimation(to, duration);
            anim.Completed += (s, _) => SystemCommands.CloseWindow(window);
            window.BeginAnimation(UIElement.OpacityProperty, anim);
        }
        #endregion

        #region Color Events

        internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, bool alpha)
        {
            ccAnim.From = !alpha ? GetPrefferedColorOver() : GetPrefferedColorOverAlpha();
            ccAnim.To = Color.FromArgb(a, r, g, b);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void MouseEnterColorEvent(byte a, byte r, byte g, byte b, Brush brush, bool alpha)
        {
            ccAnim.From = Color.FromArgb(a, r, g, b);
            ccAnim.To = !alpha ? GetPrefferedColorOver() : GetPrefferedColorOverAlpha();
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void PreviewMouseLeftButtonDownColorEvent(Brush brush, bool alpha)
        {
            if (!alpha)
            {
                ccAnim.From = GetPrefferedColorOver();
                ccAnim.To = GetPrefferedColorDown();
            }
            else
            {
                ccAnim.From = GetPrefferedColorOverAlpha();
                ccAnim.To = GetPrefferedColorDownAlpha();
            }
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, short colortheme)
        {
            switch (colortheme)
            {
                case 1:
                default:
                    ccAnim.From = Settings.Default.Blue;
                    break;
                case 2:
                    ccAnim.From = Settings.Default.Pink;
                    break;
                case 3:
                    ccAnim.From = Settings.Default.Orange;
                    break;
                case 4:
                    ccAnim.From = Settings.Default.Green;
                    break;
                case 5:
                    ccAnim.From = Settings.Default.Red;
                    break;
                case 6:
                    ccAnim.From = Settings.Default.Teal;
                    break;
                case 7:
                    ccAnim.From = Settings.Default.Aqua;
                    break;
                case 8:
                    ccAnim.From = Settings.Default.Beige;
                    break;
                case 9:
                    ccAnim.From = Settings.Default.Purple;
                    break;
                case 10:
                    ccAnim.From = Settings.Default.Cyan;
                    break;
                case 11:
                    ccAnim.From = Settings.Default.Magenta;
                    break;
                case 12:
                    ccAnim.From = Settings.Default.Grey;
                    break;
            }
            ccAnim.To = Color.FromArgb(a, r, g, b);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void MouseEnterColorEvent(byte a, byte r, byte g, byte b, Brush brush, short colortheme)
        {
            ccAnim.From = Color.FromArgb(a, r, g, b);
            switch (colortheme)
            {
                case 1:
                default:
                    ccAnim.To = Settings.Default.Blue;
                    break;
                case 2:
                    ccAnim.To = Settings.Default.Pink;
                    break;
                case 3:
                    ccAnim.To = Settings.Default.Orange;
                    break;
                case 4:
                    ccAnim.To = Settings.Default.Green;
                    break;
                case 5:
                    ccAnim.To = Settings.Default.Red;
                    break;
                case 6:
                    ccAnim.To = Settings.Default.Teal;
                    break;
                case 7:
                    ccAnim.To = Settings.Default.Aqua;
                    break;
                case 8:
                    ccAnim.To = Settings.Default.Beige;
                    break;
                case 9:
                    ccAnim.To = Settings.Default.Purple;
                    break;
                case 10:
                    ccAnim.To = Settings.Default.Cyan;
                    break;
                case 11:
                    ccAnim.To = Settings.Default.Magenta;
                    break;
                case 12:
                    ccAnim.To = Settings.Default.Grey;
                    break;
            }
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void PreviewMouseLeftButtonDownColorEvent(Brush brush, short colortheme)
        {
            switch (colortheme)
            {
                case 1:
                default:
                    ccAnim.From = Settings.Default.Blue;
                    ccAnim.To = Settings.Default.BlueMouse;
                    break;
                case 2:
                    ccAnim.From = Settings.Default.Pink;
                    ccAnim.To = Settings.Default.PinkMouse;
                    break;
                case 3:
                    ccAnim.From = Settings.Default.Orange;
                    ccAnim.To = Settings.Default.OrangeMouse;
                    break;
                case 4:
                    ccAnim.From = Settings.Default.Green;
                    ccAnim.To = Settings.Default.GreenMouse;
                    break;
                case 5:
                    ccAnim.From = Settings.Default.Red;
                    ccAnim.To = Settings.Default.RedMouse;
                    break;
                case 6:
                    ccAnim.From = Settings.Default.Teal;
                    ccAnim.To = Settings.Default.TealMouse;
                    break;
                case 7:
                    ccAnim.From = Settings.Default.Aqua;
                    ccAnim.To = Settings.Default.AquaMouse;
                    break;
                case 8:
                    ccAnim.From = Settings.Default.Beige;
                    ccAnim.To = Settings.Default.BeigeMouse;
                    break;
                case 9:
                    ccAnim.From = Settings.Default.Purple;
                    ccAnim.To = Settings.Default.PurpleMouse;
                    break;
                case 10:
                    ccAnim.From = Settings.Default.Cyan;
                    ccAnim.To = Settings.Default.CyanMouse;
                    break;
                case 11:
                    ccAnim.From = Settings.Default.Magenta;
                    ccAnim.To = Settings.Default.MagentaMouse;
                    break;
                case 12:
                    ccAnim.From = Settings.Default.Grey;
                    ccAnim.To = Settings.Default.GreyMouse;
                    break;
            }
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        #endregion

        #region Color stuff

        #region Return Color

        internal static Color GetPrefferedColorOver()
        {
            switch (Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Settings.Default.Blue;
                case 2:
                    return Settings.Default.Pink;
                case 3:
                    return Settings.Default.Orange;
                case 4:
                    return Settings.Default.Green;
                case 5:
                    return Settings.Default.Red;
                case 6:
                    return Settings.Default.Teal;
                case 7:
                    return Settings.Default.Aqua;
                case 8:
                    return Settings.Default.Beige;
                case 9:
                    return Settings.Default.Purple;
                case 10:
                    return Settings.Default.Cyan;
                case 11:
                    return Settings.Default.Magenta;
                case 12:
                    return Settings.Default.Grey;

            }
        }

        internal static Color GetPrefferedColorDown()
        {
            switch (Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Settings.Default.BlueMouse;
                case 2:
                    return Settings.Default.PinkMouse;
                case 3:
                    return Settings.Default.OrangeMouse;
                case 4:
                    return Settings.Default.GreenMouse;
                case 5:
                    return Settings.Default.RedMouse;
                case 6:
                    return Settings.Default.TealMouse;
                case 7:
                    return Settings.Default.AquaMouse;
                case 8:
                    return Settings.Default.BeigeMouse;
                case 9:
                    return Settings.Default.PurpleMouse;
                case 10:
                    return Settings.Default.CyanMouse;
                case 11:
                    return Settings.Default.MagentaMouse;
                case 12:
                    return Settings.Default.GreyMouse;
            }
        }

        #region Alpha

        internal static Color GetPrefferedColorDownAlpha()
        {
            switch (Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Color.FromArgb(
                        220,
                        Settings.Default.BlueMouse.R,
                        Settings.Default.BlueMouse.G,
                        Settings.Default.BlueMouse.B);
                case 2:
                    return Color.FromArgb(
                        220,
                        Settings.Default.PinkMouse.R,
                        Settings.Default.PinkMouse.G,
                        Settings.Default.PinkMouse.B);
                case 3:
                    return Color.FromArgb(
                        220,
                        Settings.Default.OrangeMouse.R,
                        Settings.Default.OrangeMouse.G,
                        Settings.Default.OrangeMouse.B);
                case 4:
                    return Color.FromArgb(
                        220,
                        Settings.Default.GreenMouse.R,
                        Settings.Default.GreenMouse.G,
                        Settings.Default.GreenMouse.B);
                case 5:
                    return Color.FromArgb(
                        220,
                        Settings.Default.RedMouse.R,
                        Settings.Default.RedMouse.G,
                        Settings.Default.RedMouse.B);
                case 6:
                    return Color.FromArgb(
                        220,
                        Settings.Default.TealMouse.R,
                        Settings.Default.TealMouse.G,
                        Settings.Default.TealMouse.B);
                case 7:
                    return Color.FromArgb(
                        220,
                        Settings.Default.AquaMouse.R,
                        Settings.Default.AquaMouse.G,
                        Settings.Default.AquaMouse.B);
                case 8:
                    return Color.FromArgb(
                        220,
                        Settings.Default.BeigeMouse.R,
                        Settings.Default.BeigeMouse.G,
                        Settings.Default.BeigeMouse.B);
                case 9:
                    return Color.FromArgb(
                        220,
                        Settings.Default.PurpleMouse.R,
                        Settings.Default.PurpleMouse.G,
                        Settings.Default.PurpleMouse.B);
                case 10:
                    return Color.FromArgb(
                        220,
                        Settings.Default.CyanMouse.R,
                        Settings.Default.CyanMouse.G,
                        Settings.Default.CyanMouse.B);
                case 11:
                    return Color.FromArgb(
                        220,
                        Settings.Default.MagentaMouse.R,
                        Settings.Default.MagentaMouse.G,
                        Settings.Default.MagentaMouse.B);
                case 12:
                    return Color.FromArgb(
                        220,
                        Settings.Default.GreyMouse.R,
                        Settings.Default.GreyMouse.G,
                        Settings.Default.GreyMouse.B);
            }
        }

        internal static Color GetPrefferedColorOverAlpha()
        {
            switch (Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Blue.R,
                        Settings.Default.Blue.G,
                        Settings.Default.Blue.B);
                case 2:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Pink.R,
                        Settings.Default.Pink.G,
                        Settings.Default.Pink.B);
                case 3:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Orange.R,
                        Settings.Default.Orange.G,
                        Settings.Default.Orange.B);
                case 4:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Green.R,
                        Settings.Default.Green.G,
                        Settings.Default.Green.B);
                case 5:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Red.R,
                        Settings.Default.Red.G,
                        Settings.Default.Red.B);
                case 6:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Teal.R,
                        Settings.Default.Teal.G,
                        Settings.Default.Teal.B);
                case 7:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Aqua.R,
                        Settings.Default.Aqua.G,
                        Settings.Default.Aqua.B);
                case 8:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Beige.R,
                        Settings.Default.Beige.G,
                        Settings.Default.Beige.B);
                case 9:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Purple.R,
                        Settings.Default.Purple.G,
                        Settings.Default.Purple.B);
                case 10:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Cyan.R,
                        Settings.Default.Cyan.G,
                        Settings.Default.Cyan.B);
                case 11:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Magenta.R,
                        Settings.Default.Magenta.G,
                        Settings.Default.Magenta.B);
                case 12:
                    return Color.FromArgb(
                        220,
                        Settings.Default.Grey.R,
                        Settings.Default.Grey.G,
                        Settings.Default.Grey.B);
            }
        }

        #endregion

        #endregion

        #endregion

        #region Size Animation

        internal static void HoverSizeAnim(UIElement element, bool unHover, double from, double to)
        {
            var da = new DoubleAnimation();
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
            da.AccelerationRatio = 0.4;
            da.DecelerationRatio = 0.6;

            element.BeginAnimation(Rectangle.WidthProperty, da);
            element.BeginAnimation(Rectangle.HeightProperty, da);
        }

        #endregion

    }
}
