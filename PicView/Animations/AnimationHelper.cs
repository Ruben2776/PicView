using PicView.ChangeImage;
using PicView.Properties;
using PicView.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.Animations
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
            }
        }

        internal static bool Fade(UIElement element, Duration duration, TimeSpan begintime, double from, double to)
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
                return false;
            }
            return true;
        }

        #endregion Fade

        #region Color Events
        internal static void MouseEnterFadeColor(Brush brush)
        {
            colorAnimation.From = (Color)Application.Current.Resources["FadeColor"]; 
            colorAnimation.To = (Color)Application.Current.Resources["BackgroundSubtleHighlight"];
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void MouseLeaveFadeColor(Brush brush)
        {
            colorAnimation.From = (Color)Application.Current.Resources["BackgroundSubtleHighlight"];
            colorAnimation.To = (Color)Application.Current.Resources["FadeColor"];
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }
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
            colorAnimation.From = GetPrefferedColor();
            colorAnimation.To = Color.FromArgb(a, r, g, b);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void MouseOverColorEvent(byte a, byte r, byte g, byte b, Brush brush, bool alpha)
        {
            colorAnimation.From = Color.FromArgb(a, r, g, b);
            colorAnimation.To = GetPrefferedColor();
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void LightThemeMouseEvent(UIElement uIElement, Brush brush)
        {
            var c = (Color)Application.Current.Resources["IconColor"];

            uIElement.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromRgb(c.R, c.G, c.B);
                colorAnimation.To = Colors.White;
                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            uIElement.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromRgb(c.R, c.G, c.B);
                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
        }

        internal static void MouseLeaveColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colortheme)
        {
            colorAnimation.From = colortheme switch
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
            colorAnimation.To = Color.FromArgb(a, r, g, b);
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        internal static void MouseEnterColorEvent(byte a, byte r, byte g, byte b, Brush brush, int colortheme)
        {
            colorAnimation.From = Color.FromArgb(a, r, g, b);
            colorAnimation.To = colortheme switch
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
            brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        #endregion Color Events

        #region Color stuff

        #region Return Color

        internal static Color GetPrefferedColor()
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

        #endregion Return Color

        #endregion Color stuff

        #region Size Animation

        internal static void HoverSizeAnim(PicGalleryItem item, bool unHover, double from, double to)
        {
            if (item.Id == Navigation.FolderIndex || item.Id == PicGallery.GalleryNavigation.SelectedGalleryItem || PicGallery.GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                return;
            }
            if (item.innerborder.Width > PicGallery.GalleryNavigation.PicGalleryItem_Size_s && unHover == false)
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
                item.innerborder.Width = to;
                item.innerborder.Height = to;
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

            item.innerborder.BeginAnimation(FrameworkElement.WidthProperty, da);
            item.innerborder.BeginAnimation(FrameworkElement.HeightProperty, da);
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
}