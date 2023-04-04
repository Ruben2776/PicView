using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic.Sizing;
using System.Windows;
using System.Windows.Media;

namespace PicView.UILogic.TransformImage
{
    /// <summary>
    /// Contains static methods to manipulate the rotation and flipping of the displayed image.
    /// </summary>
    internal static class Rotation
    {
        /// <summary>
        /// Used to determine if the image has been flipped
        /// </summary>
        internal static bool IsFlipped { get; set; }

        /// <summary>
        /// Used to get rotation by degrees
        /// </summary>
        internal static double RotationAngle { get; set; }

        /// <summary>
        /// Rotates the image and moves the cursor to the new position.
        /// </summary>
        /// <param name="up">If true, rotates the image up (clockwise); otherwise, rotates it down (counterclockwise).</param>
        /// <param name="uIElement">The UIElement in which the image is displayed.</param>
        internal static async Task RotateAndMoveCursor(bool up, UIElement uIElement)
        {
            Rotate(false, up);
            if (Settings.Default.AutoFitWindow == false) { return; }

            // Move cursor after rotating
            await Task.Delay(50).ConfigureAwait(true); // Delay it, so that the move takes place after window has resized
            var p = uIElement.PointToScreen(new Point(25, 25));
            NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
        }

        /// <summary>
        /// Rotates the image.
        /// </summary>
        /// <param name="keyDown">If true, the rotation is triggered by a key down event; otherwise, it is triggered by a key up event.</param>
        /// <param name="up">If true, rotates the image up (clockwise); otherwise, rotates it down (counterclockwise).</param>
        internal static void Rotate(bool keyDown, bool up)
        {
            if (keyDown)
            {
                Rotate(up ? RotationAngle + 1 : RotationAngle - 1);
            }
            else
            {
                if (IsValidRotation(RotationAngle))
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
        /// Rotates the image by 90 degrees.
        /// </summary>
        /// <param name="up">If true, rotates the image up (clockwise); otherwise, rotates it down (counterclockwise).</param>
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
        /// Rotates the image by the specified angle in degrees.
        /// </summary>
        /// <param name="degrees">The angle in degrees to rotate the image.</param>
        internal static void Rotate(double degrees)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null ||
                Settings.Default.FullscreenGalleryHorizontal == false && GalleryFunctions.IsHorizontalOpen)
            { return; }

            var rt = new RotateTransform { Angle = RotationAngle = degrees };

            ScaleImage.FitImage(ConfigureWindows.GetMainWindow.MainImage.Source.Width, ConfigureWindows.GetMainWindow.MainImage.Source.Height);

            // If it's flipped, keep it flipped when rotating
            if (IsFlipped)
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

            if (!IsFlipped)
            {
                flip.ScaleX = -1;
                IsFlipped = true;
            }
            else
            {
                flip.ScaleX = +1;
                IsFlipped = false;
            }

            rt.Angle = RotationAngle;
            tg.Children.Add(flip);
            tg.Children.Add(rt);
            ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = tg;
        }

        /// <summary>
        /// This method checks if the provided rotation angle is valid or not.
        /// A rotation angle is considered valid if it is a multiple of 90 degrees and lies within the range of 0 to 360 degrees (inclusive).
        /// If the provided angle is not valid, the method returns false. Otherwise, it returns true.
        /// </summary>
        /// <param name="rotationAngle">A double value representing the rotation angle to be checked.</param>
        /// <returns>A bool value representing whether the provided rotation angle is valid or not.</returns>
        internal static bool IsValidRotation(double rotationAngle)
        {
            rotationAngle = rotationAngle % 360;
            return rotationAngle == 0 || rotationAngle == 90 || rotationAngle == 180 || rotationAngle == 270;
        }

        /// <summary>
        /// This method returns the next rotation angle to be used based on the current rotation angle and the direction of rotation.
        /// If roundUp is true, the method returns the next multiple of 90 degrees that is greater than the current rotation angle.
        /// Otherwise, it returns the next multiple of 90 degrees that is less than the current rotation angle.
        /// The returned value is an integer representing the next rotation angle in degrees.
        /// </summary>
        /// <param name="currentDegrees">A double value representing the current rotation angle in degrees.</param>
        /// <param name="roundUp">A bool value indicating the direction of rotation. If true, the method returns the next higher multiple of 90 degrees.
        /// Otherwise, it returns the next lower multiple of 90 degrees.</param>
        /// <returns>An integer representing the next rotation angle in degrees.</returns>
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