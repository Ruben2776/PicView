using PicView.ChangeImage;
using PicView.UILogic.UserControls;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.UILogic.Animations
{
    internal static class AnimationHelper
    {
        private static readonly ColorAnimation colorAnimation = new ColorAnimation { Duration = TimeSpan.FromSeconds(.35) };

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

        internal static void MouseEnterBgTexColor(Brush brush)
        {
            colorAnimation.From = Color.FromArgb(0, 0, 0, 0);
            colorAnimation.To = (Color)Application.Current.Resources["BackgroundHoverHighlight"];
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void MouseLeaveBgTexColor(Brush brush)
        {
            colorAnimation.From = (Color)Application.Current.Resources["BackgroundHoverHighlight"];
            colorAnimation.To = Color.FromArgb(0, 0, 0, 0);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, bool alpha)
        {
            colorAnimation.From = !alpha ? GetPrefferedColorOver() : GetPrefferedColorOverAlpha();
            colorAnimation.To = Color.FromArgb(a, r, g, b);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void MouseOverColorEvent(byte a, byte r, byte g, byte b, Brush brush, bool alpha)
        {
            colorAnimation.From = Color.FromArgb(a, r, g, b);
            colorAnimation.To = !alpha ? GetPrefferedColorOver() : GetPrefferedColorOverAlpha();
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void PreviewMouseLeftButtonDownColorEvent(Brush brush, bool alpha)
        {
            if (!alpha)
            {
                colorAnimation.From = GetPrefferedColorOver();
                colorAnimation.To = GetPrefferedColorDown();
            }
            else
            {
                colorAnimation.From = GetPrefferedColorOverAlpha();
                colorAnimation.To = GetPrefferedColorDownAlpha();
            }
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colortheme)
        {
            colorAnimation.From = colortheme switch
            {
                2 => Properties.Settings.Default.Pink,
                3 => Properties.Settings.Default.Orange,
                4 => Properties.Settings.Default.Green,
                5 => Properties.Settings.Default.Red,
                6 => Properties.Settings.Default.Teal,
                7 => Properties.Settings.Default.Aqua,
                8 => Properties.Settings.Default.Golden,
                9 => Properties.Settings.Default.Purple,
                10 => Properties.Settings.Default.Cyan,
                11 => Properties.Settings.Default.Magenta,
                12 => Properties.Settings.Default.Lime,
                _ => Properties.Settings.Default.Blue,
            };
            colorAnimation.To = Color.FromArgb(a, r, g, b);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void MouseEnterColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colortheme)
        {
            colorAnimation.From = Color.FromArgb(a, r, g, b);
            colorAnimation.To = colortheme switch
            {
                2 => Properties.Settings.Default.Pink,
                3 => Properties.Settings.Default.Orange,
                4 => Properties.Settings.Default.Green,
                5 => Properties.Settings.Default.Red,
                6 => Properties.Settings.Default.Teal,
                7 => Properties.Settings.Default.Aqua,
                8 => Properties.Settings.Default.Golden,
                9 => Properties.Settings.Default.Purple,
                10 => Properties.Settings.Default.Cyan,
                11 => Properties.Settings.Default.Magenta,
                12 => Properties.Settings.Default.Lime,
                _ => Properties.Settings.Default.Blue,
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void PreviewMouseLeftButtonDownColorEvent(Brush brush, int colortheme)
        {
            switch (colortheme)
            {
                case 1:
                default:
                    colorAnimation.From = Properties.Settings.Default.Blue;
                    colorAnimation.To = Properties.Settings.Default.BlueAlpha;
                    break;

                case 2:
                    colorAnimation.From = Properties.Settings.Default.Pink;
                    colorAnimation.To = Properties.Settings.Default.PinkAlpha;
                    break;

                case 3:
                    colorAnimation.From = Properties.Settings.Default.Orange;
                    colorAnimation.To = Properties.Settings.Default.OrangeAlpha;
                    break;

                case 4:
                    colorAnimation.From = Properties.Settings.Default.Green;
                    colorAnimation.To = Properties.Settings.Default.GreenAlpha;
                    break;

                case 5:
                    colorAnimation.From = Properties.Settings.Default.Red;
                    colorAnimation.To = Properties.Settings.Default.RedAlpha;
                    break;

                case 6:
                    colorAnimation.From = Properties.Settings.Default.Teal;
                    colorAnimation.To = Properties.Settings.Default.TealAlpha;
                    break;

                case 7:
                    colorAnimation.From = Properties.Settings.Default.Aqua;
                    colorAnimation.To = Properties.Settings.Default.AquaAlpha;
                    break;

                case 8:
                    colorAnimation.From = Properties.Settings.Default.Golden;
                    colorAnimation.To = Properties.Settings.Default.GoldenAlpha;
                    break;

                case 9:
                    colorAnimation.From = Properties.Settings.Default.Purple;
                    colorAnimation.To = Properties.Settings.Default.PurpleAlpha;
                    break;

                case 10:
                    colorAnimation.From = Properties.Settings.Default.Cyan;
                    colorAnimation.To = Properties.Settings.Default.CyanAlpha;
                    break;

                case 11:
                    colorAnimation.From = Properties.Settings.Default.Magenta;
                    colorAnimation.To = Properties.Settings.Default.MagentaAlpha;
                    break;

                case 12:
                    colorAnimation.From = Properties.Settings.Default.Lime;
                    colorAnimation.To = Properties.Settings.Default.LimeAlpha;
                    break;
            }
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        #endregion Color Events

        #region Color stuff

        #region Return Color

        internal static Color GetPrefferedColorOver()
        {
            return Properties.Settings.Default.ColorTheme switch
            {
                2 => Properties.Settings.Default.Pink,
                3 => Properties.Settings.Default.Orange,
                4 => Properties.Settings.Default.Green,
                5 => Properties.Settings.Default.Red,
                6 => Properties.Settings.Default.Teal,
                7 => Properties.Settings.Default.Aqua,
                8 => Properties.Settings.Default.Golden,
                9 => Properties.Settings.Default.Purple,
                10 => Properties.Settings.Default.Cyan,
                11 => Properties.Settings.Default.Magenta,
                12 => Properties.Settings.Default.Lime,
                _ => Properties.Settings.Default.Blue,
            };
        }

        internal static Color GetPrefferedColorDown()
        {
            return Properties.Settings.Default.ColorTheme switch
            {
                2 => Properties.Settings.Default.PinkAlpha,
                3 => Properties.Settings.Default.OrangeAlpha,
                4 => Properties.Settings.Default.GreenAlpha,
                5 => Properties.Settings.Default.RedAlpha,
                6 => Properties.Settings.Default.TealAlpha,
                7 => Properties.Settings.Default.AquaAlpha,
                8 => Properties.Settings.Default.GoldenAlpha,
                9 => Properties.Settings.Default.PurpleAlpha,
                10 => Properties.Settings.Default.CyanAlpha,
                11 => Properties.Settings.Default.MagentaAlpha,
                12 => Properties.Settings.Default.LimeAlpha,
                _ => Properties.Settings.Default.BlueAlpha,
            };
        }

        #region Alpha

        internal static Color GetPrefferedColorDownAlpha()
        {
            return Properties.Settings.Default.ColorTheme switch
            {
                2 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.PinkAlpha.R,
                    Properties.Settings.Default.PinkAlpha.G,
                    Properties.Settings.Default.PinkAlpha.B),

                3 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.OrangeAlpha.R,
                    Properties.Settings.Default.OrangeAlpha.G,
                    Properties.Settings.Default.OrangeAlpha.B),

                4 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.GreenAlpha.R,
                    Properties.Settings.Default.GreenAlpha.G,
                    Properties.Settings.Default.GreenAlpha.B),

                5 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.RedAlpha.R,
                    Properties.Settings.Default.RedAlpha.G,
                    Properties.Settings.Default.RedAlpha.B),
                
                6 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.TealAlpha.R,
                    Properties.Settings.Default.TealAlpha.G,
                    Properties.Settings.Default.TealAlpha.B),

                7 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.AquaAlpha.R,
                    Properties.Settings.Default.AquaAlpha.G,
                    Properties.Settings.Default.AquaAlpha.B),

                8 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.GoldenAlpha.R,
                    Properties.Settings.Default.GoldenAlpha.G,
                    Properties.Settings.Default.GoldenAlpha.B),

                9 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.PurpleAlpha.R,
                    Properties.Settings.Default.PurpleAlpha.G,
                    Properties.Settings.Default.PurpleAlpha.B),

                10 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.CyanAlpha.R,
                    Properties.Settings.Default.CyanAlpha.G,
                    Properties.Settings.Default.CyanAlpha.B),

                11 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.MagentaAlpha.R,
                    Properties.Settings.Default.MagentaAlpha.G,
                    Properties.Settings.Default.MagentaAlpha.B),

                12 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.LimeAlpha.R,
                    Properties.Settings.Default.LimeAlpha.G,
                    Properties.Settings.Default.LimeAlpha.B),

                _ => Color.FromArgb(
                    160,
                    Properties.Settings.Default.BlueAlpha.R,
                    Properties.Settings.Default.BlueAlpha.G,
                    Properties.Settings.Default.BlueAlpha.B),
            };
        }

        internal static Color GetPrefferedColorOverAlpha()
        {
            return Properties.Settings.Default.ColorTheme switch
            {
                2 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Pink.R,
                    Properties.Settings.Default.Pink.G,
                    Properties.Settings.Default.Pink.B),

                3 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Orange.R,
                    Properties.Settings.Default.Orange.G,
                    Properties.Settings.Default.Orange.B),

                4 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Green.R,
                    Properties.Settings.Default.Green.G,
                    Properties.Settings.Default.Green.B),

                5 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Red.R,
                    Properties.Settings.Default.Red.G,
                    Properties.Settings.Default.Red.B),

                6 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Teal.R,
                    Properties.Settings.Default.Teal.G,
                    Properties.Settings.Default.Teal.B),

                7 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Aqua.R,
                    Properties.Settings.Default.Aqua.G,
                    Properties.Settings.Default.Aqua.B),

                8 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Golden.R,
                    Properties.Settings.Default.Golden.G,
                    Properties.Settings.Default.Golden.B),

                9 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Purple.R,
                    Properties.Settings.Default.Purple.G,
                    Properties.Settings.Default.Purple.B),

                10 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Cyan.R,
                    Properties.Settings.Default.Cyan.G,
                    Properties.Settings.Default.Cyan.B),

                11 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Magenta.R,
                    Properties.Settings.Default.Magenta.G,
                    Properties.Settings.Default.Magenta.B),

                12 => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Lime.R,
                    Properties.Settings.Default.Lime.G,
                    Properties.Settings.Default.Lime.B),

                _ => Color.FromArgb(
                    160,
                    Properties.Settings.Default.Blue.R,
                    Properties.Settings.Default.Blue.G,
                    Properties.Settings.Default.Blue.B),
            };
        }

        #endregion Alpha

        #endregion Return Color

        #endregion Color stuff

        #region Size Animation

        internal static void HoverSizeAnim(PicGalleryItem item, bool unHover, double from, double to)
        {
            if (item.Id == Navigation.FolderIndex)
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