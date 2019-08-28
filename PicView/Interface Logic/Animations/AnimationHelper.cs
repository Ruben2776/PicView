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
                    ccAnim.From = Properties.Settings.Default.Blue;
                    break;
                case 2:
                    ccAnim.From = Properties.Settings.Default.Pink;
                    break;
                case 3:
                    ccAnim.From = Properties.Settings.Default.Orange;
                    break;
                case 4:
                    ccAnim.From = Properties.Settings.Default.Green;
                    break;
                case 5:
                    ccAnim.From = Properties.Settings.Default.Red;
                    break;
                case 6:
                    ccAnim.From = Properties.Settings.Default.Teal;
                    break;
                case 7:
                    ccAnim.From = Properties.Settings.Default.Aqua;
                    break;
                case 8:
                    ccAnim.From = Properties.Settings.Default.Beige;
                    break;
                case 9:
                    ccAnim.From = Properties.Settings.Default.Purple;
                    break;
                case 10:
                    ccAnim.From = Properties.Settings.Default.Cyan;
                    break;
                case 11:
                    ccAnim.From = Properties.Settings.Default.Magenta;
                    break;
                case 12:
                    ccAnim.From = Properties.Settings.Default.Grey;
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
                    ccAnim.To = Properties.Settings.Default.Blue;
                    break;
                case 2:
                    ccAnim.To = Properties.Settings.Default.Pink;
                    break;
                case 3:
                    ccAnim.To = Properties.Settings.Default.Orange;
                    break;
                case 4:
                    ccAnim.To = Properties.Settings.Default.Green;
                    break;
                case 5:
                    ccAnim.To = Properties.Settings.Default.Red;
                    break;
                case 6:
                    ccAnim.To = Properties.Settings.Default.Teal;
                    break;
                case 7:
                    ccAnim.To = Properties.Settings.Default.Aqua;
                    break;
                case 8:
                    ccAnim.To = Properties.Settings.Default.Beige;
                    break;
                case 9:
                    ccAnim.To = Properties.Settings.Default.Purple;
                    break;
                case 10:
                    ccAnim.To = Properties.Settings.Default.Cyan;
                    break;
                case 11:
                    ccAnim.To = Properties.Settings.Default.Magenta;
                    break;
                case 12:
                    ccAnim.To = Properties.Settings.Default.Grey;
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
                    ccAnim.From = Properties.Settings.Default.Blue;
                    ccAnim.To = Properties.Settings.Default.BlueMouse;
                    break;
                case 2:
                    ccAnim.From = Properties.Settings.Default.Pink;
                    ccAnim.To = Properties.Settings.Default.PinkMouse;
                    break;
                case 3:
                    ccAnim.From = Properties.Settings.Default.Orange;
                    ccAnim.To = Properties.Settings.Default.OrangeMouse;
                    break;
                case 4:
                    ccAnim.From = Properties.Settings.Default.Green;
                    ccAnim.To = Properties.Settings.Default.GreenMouse;
                    break;
                case 5:
                    ccAnim.From = Properties.Settings.Default.Red;
                    ccAnim.To = Properties.Settings.Default.RedMouse;
                    break;
                case 6:
                    ccAnim.From = Properties.Settings.Default.Teal;
                    ccAnim.To = Properties.Settings.Default.TealMouse;
                    break;
                case 7:
                    ccAnim.From = Properties.Settings.Default.Aqua;
                    ccAnim.To = Properties.Settings.Default.AquaMouse;
                    break;
                case 8:
                    ccAnim.From = Properties.Settings.Default.Beige;
                    ccAnim.To = Properties.Settings.Default.BeigeMouse;
                    break;
                case 9:
                    ccAnim.From = Properties.Settings.Default.Purple;
                    ccAnim.To = Properties.Settings.Default.PurpleMouse;
                    break;
                case 10:
                    ccAnim.From = Properties.Settings.Default.Cyan;
                    ccAnim.To = Properties.Settings.Default.CyanMouse;
                    break;
                case 11:
                    ccAnim.From = Properties.Settings.Default.Magenta;
                    ccAnim.To = Properties.Settings.Default.MagentaMouse;
                    break;
                case 12:
                    ccAnim.From = Properties.Settings.Default.Grey;
                    ccAnim.To = Properties.Settings.Default.GreyMouse;
                    break;
            }
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        #endregion

        #region Color stuff

        #region Return Color

        internal static Color GetPrefferedColorOver()
        {
            switch (Properties.Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Properties.Settings.Default.Blue;
                case 2:
                    return Properties.Settings.Default.Pink;
                case 3:
                    return Properties.Settings.Default.Orange;
                case 4:
                    return Properties.Settings.Default.Green;
                case 5:
                    return Properties.Settings.Default.Red;
                case 6:
                    return Properties.Settings.Default.Teal;
                case 7:
                    return Properties.Settings.Default.Aqua;
                case 8:
                    return Properties.Settings.Default.Beige;
                case 9:
                    return Properties.Settings.Default.Purple;
                case 10:
                    return Properties.Settings.Default.Cyan;
                case 11:
                    return Properties.Settings.Default.Magenta;
                case 12:
                    return Properties.Settings.Default.Grey;

            }
        }

        internal static Color GetPrefferedColorDown()
        {
            switch (Properties.Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Properties.Settings.Default.BlueMouse;
                case 2:
                    return Properties.Settings.Default.PinkMouse;
                case 3:
                    return Properties.Settings.Default.OrangeMouse;
                case 4:
                    return Properties.Settings.Default.GreenMouse;
                case 5:
                    return Properties.Settings.Default.RedMouse;
                case 6:
                    return Properties.Settings.Default.TealMouse;
                case 7:
                    return Properties.Settings.Default.AquaMouse;
                case 8:
                    return Properties.Settings.Default.BeigeMouse;
                case 9:
                    return Properties.Settings.Default.PurpleMouse;
                case 10:
                    return Properties.Settings.Default.CyanMouse;
                case 11:
                    return Properties.Settings.Default.MagentaMouse;
                case 12:
                    return Properties.Settings.Default.GreyMouse;
            }
        }

        #region Alpha

        internal static Color GetPrefferedColorDownAlpha()
        {
            switch (Properties.Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.BlueMouse.R,
                        Properties.Settings.Default.BlueMouse.G,
                        Properties.Settings.Default.BlueMouse.B);
                case 2:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.PinkMouse.R,
                        Properties.Settings.Default.PinkMouse.G,
                        Properties.Settings.Default.PinkMouse.B);
                case 3:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.OrangeMouse.R,
                        Properties.Settings.Default.OrangeMouse.G,
                        Properties.Settings.Default.OrangeMouse.B);
                case 4:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.GreenMouse.R,
                        Properties.Settings.Default.GreenMouse.G,
                        Properties.Settings.Default.GreenMouse.B);
                case 5:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.RedMouse.R,
                        Properties.Settings.Default.RedMouse.G,
                        Properties.Settings.Default.RedMouse.B);
                case 6:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.TealMouse.R,
                        Properties.Settings.Default.TealMouse.G,
                        Properties.Settings.Default.TealMouse.B);
                case 7:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.AquaMouse.R,
                        Properties.Settings.Default.AquaMouse.G,
                        Properties.Settings.Default.AquaMouse.B);
                case 8:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.BeigeMouse.R,
                        Properties.Settings.Default.BeigeMouse.G,
                        Properties.Settings.Default.BeigeMouse.B);
                case 9:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.PurpleMouse.R,
                        Properties.Settings.Default.PurpleMouse.G,
                        Properties.Settings.Default.PurpleMouse.B);
                case 10:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.CyanMouse.R,
                        Properties.Settings.Default.CyanMouse.G,
                        Properties.Settings.Default.CyanMouse.B);
                case 11:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.MagentaMouse.R,
                        Properties.Settings.Default.MagentaMouse.G,
                        Properties.Settings.Default.MagentaMouse.B);
                case 12:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.GreyMouse.R,
                        Properties.Settings.Default.GreyMouse.G,
                        Properties.Settings.Default.GreyMouse.B);
            }
        }

        internal static Color GetPrefferedColorOverAlpha()
        {
            switch (Properties.Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Blue.R,
                        Properties.Settings.Default.Blue.G,
                        Properties.Settings.Default.Blue.B);
                case 2:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Pink.R,
                        Properties.Settings.Default.Pink.G,
                        Properties.Settings.Default.Pink.B);
                case 3:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Orange.R,
                        Properties.Settings.Default.Orange.G,
                        Properties.Settings.Default.Orange.B);
                case 4:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Green.R,
                        Properties.Settings.Default.Green.G,
                        Properties.Settings.Default.Green.B);
                case 5:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Red.R,
                        Properties.Settings.Default.Red.G,
                        Properties.Settings.Default.Red.B);
                case 6:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Teal.R,
                        Properties.Settings.Default.Teal.G,
                        Properties.Settings.Default.Teal.B);
                case 7:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Aqua.R,
                        Properties.Settings.Default.Aqua.G,
                        Properties.Settings.Default.Aqua.B);
                case 8:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Beige.R,
                        Properties.Settings.Default.Beige.G,
                        Properties.Settings.Default.Beige.B);
                case 9:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Purple.R,
                        Properties.Settings.Default.Purple.G,
                        Properties.Settings.Default.Purple.B);
                case 10:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Cyan.R,
                        Properties.Settings.Default.Cyan.G,
                        Properties.Settings.Default.Cyan.B);
                case 11:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Magenta.R,
                        Properties.Settings.Default.Magenta.G,
                        Properties.Settings.Default.Magenta.B);
                case 12:
                    return Color.FromArgb(
                        220,
                        Properties.Settings.Default.Grey.R,
                        Properties.Settings.Default.Grey.G,
                        Properties.Settings.Default.Grey.B);
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
