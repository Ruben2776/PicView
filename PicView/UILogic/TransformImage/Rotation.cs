using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic.Sizing;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PicView.UILogic.TransformImage
{
    internal static class Rotation
    {
        internal static bool Flipped { get; set; }

        /// <summary>
        /// Used to get and set image rotation by degrees
        /// </summary>
        internal static double RotationAngle { get; set; }

        internal static async Task RotateAndMoveCursor(bool up, UIElement uIElement)
        {
            Rotate(false, up);
            if (Settings.Default.AutoFitWindow == false) { return; }

            // Move cursor after rotating
            await Task.Delay(50).ConfigureAwait(true); // Delay it, so that the move takes place after window has resized
            var p = uIElement.PointToScreen(new Point(25, 25));
            NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
        }

        internal static void Rotate(bool keyDown, bool up)
        {
            if (keyDown)
            {
                Rotate(up ? RotationAngle + 1 : RotationAngle - 1);
            }
            else
            {
                if (Rotation.IsValidRotation(RotationAngle))
                {
                    Rotate(up);
                }
                else
                {
                    Rotate(NextRotationAngle(RotationAngle, up));
                }
            }
        }

        /// <summary>
        /// Rotates left or right
        /// </summary>
        /// <param name="right"></param>
        internal static void Rotate(bool up)
        {
            if (up)
            {
                RotationAngle += 90;
                if (RotationAngle >= 360) { RotationAngle -= 360; }
            }
            else
            {
                RotationAngle -= 90;
                if (RotationAngle < 0) { RotationAngle += 360; }
            }

            Rotate(RotationAngle);
        }

        /// <summary>
        /// Rotates the image the specified degrees
        /// </summary>
        /// <param name="r"></param>
        internal static void Rotate(double r)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null ||
                Settings.Default.FullscreenGalleryHorizontal == false && GalleryFunctions.IsHorizontalOpen)
            { return; }

            var rt = new RotateTransform { Angle = RotationAngle = r };

            ScaleImage.FitImage(ConfigureWindows.GetMainWindow.MainImage.Source.Width, ConfigureWindows.GetMainWindow.MainImage.Source.Height);

            // If it's flipped, keep it flipped when rotating
            if (Flipped)
            {
                var tg = new TransformGroup();
                var flip = new ScaleTransform { ScaleX = -1 };
                tg.Children.Add(flip);
                tg.Children.Add(rt);
                ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = tg;
            }
            else
            {
                ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = rt;
            }
        }

        /// <summary>
        /// Flips the image
        /// </summary>
        internal static void Flip()
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null)
            {
                return;
            }

            var rt = new RotateTransform();
            var flip = new ScaleTransform();
            var tg = new TransformGroup();

            if (!Flipped)
            {
                flip.ScaleX = -1;
                Flipped = true;
            }
            else
            {
                flip.ScaleX = +1;
                Flipped = false;
            }

            rt.Angle = RotationAngle;
            tg.Children.Add(flip);
            tg.Children.Add(rt);
            ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = tg;
        }

        internal static bool IsValidRotation(double rotationAngle)
        {
            rotationAngle = rotationAngle % 360;
            return rotationAngle == 0 || rotationAngle == 90 || rotationAngle == 180 || rotationAngle == 270;
        }

        internal static int NextRotationAngle(double currentDegrees, bool roundUp)
        {
            int nearestMultipleOf90 = (int)Math.Round(currentDegrees / 90.0) * 90;
            int nextRotationAngle = nearestMultipleOf90;

            if (roundUp)
            {
                if (nearestMultipleOf90 < 360)
                {
                    nextRotationAngle = nearestMultipleOf90 + 90;
                }
                else
                {
                    nextRotationAngle = 0;
                }
            }
            else
            {
                if (nearestMultipleOf90 > 0)
                {
                    nextRotationAngle = nearestMultipleOf90 - 90;
                }
                else
                {
                    nextRotationAngle = 270;
                }
            }

            return nextRotationAngle;
        }
    }
}