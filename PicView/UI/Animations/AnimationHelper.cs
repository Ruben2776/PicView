using PicView.Library;
using PicView.UI.UserControls;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.UI.Animations
{
    internal static class AnimationHelper
    {
        //private static DoubleAnimation da = new DoubleAnimation();
        private static readonly ColorAnimation ccAnim = new ColorAnimation { Duration = TimeSpan.FromSeconds(.35) };

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
                return;
            }
        }

        internal static void Fade(UIElement element, Duration duration, TimeSpan begintime, double from, double to)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                BeginTime = begintime,
                Duration = duration
            };

            try
            {
                element.BeginAnimation(UIElement.OpacityProperty, da);
            }
            catch (Exception)
            {
                return;
            }
        }

        #endregion Fade

        #region Color Events

        internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, bool alpha)
        {
            ccAnim.From = !alpha ? GetPrefferedColorOver() : GetPrefferedColorOverAlpha();
            ccAnim.To = Color.FromArgb(a, r, g, b);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void MouseOverColorEvent(byte a, byte r, byte g, byte b, Brush brush, bool alpha)
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

        internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colortheme)
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

        internal static void MouseEnterColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colortheme)
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

        internal static void PreviewMouseLeftButtonDownColorEvent(Brush brush, int colortheme)
        {
            switch (colortheme)
            {
                case 1:
                default:
                    ccAnim.From = Properties.Settings.Default.Blue;
                    ccAnim.To = Properties.Settings.Default.BlueAlpha;
                    break;

                case 2:
                    ccAnim.From = Properties.Settings.Default.Pink;
                    ccAnim.To = Properties.Settings.Default.PinkAlpha;
                    break;

                case 3:
                    ccAnim.From = Properties.Settings.Default.Orange;
                    ccAnim.To = Properties.Settings.Default.OrangeAlpha;
                    break;

                case 4:
                    ccAnim.From = Properties.Settings.Default.Green;
                    ccAnim.To = Properties.Settings.Default.GreenAlpha;
                    break;

                case 5:
                    ccAnim.From = Properties.Settings.Default.Red;
                    ccAnim.To = Properties.Settings.Default.RedAlpha;
                    break;

                case 6:
                    ccAnim.From = Properties.Settings.Default.Teal;
                    ccAnim.To = Properties.Settings.Default.TealAlpha;
                    break;

                case 7:
                    ccAnim.From = Properties.Settings.Default.Aqua;
                    ccAnim.To = Properties.Settings.Default.AquaAlpha;
                    break;

                case 8:
                    ccAnim.From = Properties.Settings.Default.Beige;
                    ccAnim.To = Properties.Settings.Default.BeigeAlpha;
                    break;

                case 9:
                    ccAnim.From = Properties.Settings.Default.Purple;
                    ccAnim.To = Properties.Settings.Default.PurpleAlpha;
                    break;

                case 10:
                    ccAnim.From = Properties.Settings.Default.Cyan;
                    ccAnim.To = Properties.Settings.Default.CyanAlpha;
                    break;

                case 11:
                    ccAnim.From = Properties.Settings.Default.Magenta;
                    ccAnim.To = Properties.Settings.Default.MagentaAlpha;
                    break;

                case 12:
                    ccAnim.From = Properties.Settings.Default.Grey;
                    ccAnim.To = Properties.Settings.Default.GreyAlpha;
                    break;
            }
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void MouseEnterBgColor(Brush brush)
        {
            ccAnim.From = Color.FromArgb(0, 0, 0, 0);
            ccAnim.To = UI.ConfigColors.backgroundBorderColor;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void MouseLeaveBgColor(Brush brush)
        {
            ccAnim.From = UI.ConfigColors.backgroundBorderColor;
            ccAnim.To = Color.FromArgb(0, 0, 0, 0);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void MouseEnterBgTexColor(Brush brush)
        {
            ccAnim.From = Color.FromArgb(0, 0, 0, 0);
            ccAnim.To = Color.FromArgb(100, 75, 75, 75);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        internal static void MouseLeaveBgTexColor(Brush brush)
        {
            ccAnim.From = Color.FromArgb(100, 75, 75, 75);
            ccAnim.To = Color.FromArgb(0, 0, 0, 0);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
        }

        #endregion Color Events

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
                    return Properties.Settings.Default.BlueAlpha;

                case 2:
                    return Properties.Settings.Default.PinkAlpha;

                case 3:
                    return Properties.Settings.Default.OrangeAlpha;

                case 4:
                    return Properties.Settings.Default.GreenAlpha;

                case 5:
                    return Properties.Settings.Default.RedAlpha;

                case 6:
                    return Properties.Settings.Default.TealAlpha;

                case 7:
                    return Properties.Settings.Default.AquaAlpha;

                case 8:
                    return Properties.Settings.Default.BeigeAlpha;

                case 9:
                    return Properties.Settings.Default.PurpleAlpha;

                case 10:
                    return Properties.Settings.Default.CyanAlpha;

                case 11:
                    return Properties.Settings.Default.MagentaAlpha;

                case 12:
                    return Properties.Settings.Default.GreyAlpha;
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
                        160,
                        Properties.Settings.Default.BlueAlpha.R,
                        Properties.Settings.Default.BlueAlpha.G,
                        Properties.Settings.Default.BlueAlpha.B);

                case 2:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.PinkAlpha.R,
                        Properties.Settings.Default.PinkAlpha.G,
                        Properties.Settings.Default.PinkAlpha.B);

                case 3:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.OrangeAlpha.R,
                        Properties.Settings.Default.OrangeAlpha.G,
                        Properties.Settings.Default.OrangeAlpha.B);

                case 4:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.GreenAlpha.R,
                        Properties.Settings.Default.GreenAlpha.G,
                        Properties.Settings.Default.GreenAlpha.B);

                case 5:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.RedAlpha.R,
                        Properties.Settings.Default.RedAlpha.G,
                        Properties.Settings.Default.RedAlpha.B);

                case 6:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.TealAlpha.R,
                        Properties.Settings.Default.TealAlpha.G,
                        Properties.Settings.Default.TealAlpha.B);

                case 7:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.AquaAlpha.R,
                        Properties.Settings.Default.AquaAlpha.G,
                        Properties.Settings.Default.AquaAlpha.B);

                case 8:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.BeigeAlpha.R,
                        Properties.Settings.Default.BeigeAlpha.G,
                        Properties.Settings.Default.BeigeAlpha.B);

                case 9:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.PurpleAlpha.R,
                        Properties.Settings.Default.PurpleAlpha.G,
                        Properties.Settings.Default.PurpleAlpha.B);

                case 10:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.CyanAlpha.R,
                        Properties.Settings.Default.CyanAlpha.G,
                        Properties.Settings.Default.CyanAlpha.B);

                case 11:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.MagentaAlpha.R,
                        Properties.Settings.Default.MagentaAlpha.G,
                        Properties.Settings.Default.MagentaAlpha.B);

                case 12:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.GreyAlpha.R,
                        Properties.Settings.Default.GreyAlpha.G,
                        Properties.Settings.Default.GreyAlpha.B);
            }
        }

        internal static Color GetPrefferedColorOverAlpha()
        {
            switch (Properties.Settings.Default.ColorTheme)
            {
                case 1:
                default:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Blue.R,
                        Properties.Settings.Default.Blue.G,
                        Properties.Settings.Default.Blue.B);

                case 2:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Pink.R,
                        Properties.Settings.Default.Pink.G,
                        Properties.Settings.Default.Pink.B);

                case 3:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Orange.R,
                        Properties.Settings.Default.Orange.G,
                        Properties.Settings.Default.Orange.B);

                case 4:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Green.R,
                        Properties.Settings.Default.Green.G,
                        Properties.Settings.Default.Green.B);

                case 5:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Red.R,
                        Properties.Settings.Default.Red.G,
                        Properties.Settings.Default.Red.B);

                case 6:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Teal.R,
                        Properties.Settings.Default.Teal.G,
                        Properties.Settings.Default.Teal.B);

                case 7:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Aqua.R,
                        Properties.Settings.Default.Aqua.G,
                        Properties.Settings.Default.Aqua.B);

                case 8:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Beige.R,
                        Properties.Settings.Default.Beige.G,
                        Properties.Settings.Default.Beige.B);

                case 9:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Purple.R,
                        Properties.Settings.Default.Purple.G,
                        Properties.Settings.Default.Purple.B);

                case 10:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Cyan.R,
                        Properties.Settings.Default.Cyan.G,
                        Properties.Settings.Default.Cyan.B);

                case 11:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Magenta.R,
                        Properties.Settings.Default.Magenta.G,
                        Properties.Settings.Default.Magenta.B);

                case 12:
                    return Color.FromArgb(
                        160,
                        Properties.Settings.Default.Grey.R,
                        Properties.Settings.Default.Grey.G,
                        Properties.Settings.Default.Grey.B);
            }
        }

        #endregion Alpha

        #endregion Return Color

        #endregion Color stuff

        #region Size Animation

        internal static void HoverSizeAnim(PicGalleryItem item, bool unHover, double from, double to)
        {
            if (item.Id == Fields.FolderIndex)
            {
                return;
            }

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

            item.innerborder.BeginAnimation(FrameworkElement.WidthProperty, da);
            item.innerborder.BeginAnimation(FrameworkElement.HeightProperty, da);
        }

        #endregion Size Animation
    }
}