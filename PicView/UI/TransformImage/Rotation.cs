using PicView.UI.PicGallery;
using PicView.UI.Sizing;
using System.Windows.Media;
using static PicView.Library.Fields;

namespace PicView.UI.TransformImage
{
    internal static class Rotation
    {
        /// <summary>
        /// Rotates the image the specified degrees
        /// </summary>
        /// <param name="r"></param>
        internal static void Rotate(int r)
        {
            if (mainWindow.img.Source == null)
            {
                return;
            }

            var rt = new RotateTransform { Angle = Rotateint = r };

            ScaleImage.TryFitImage();

            // If it's flipped, keep it flipped when rotating
            if (Flipped)
            {
                var tg = new TransformGroup();
                var flip = new ScaleTransform { ScaleX = -1 };
                tg.Children.Add(flip);
                tg.Children.Add(rt);
                mainWindow.img.LayoutTransform = tg;
            }
            else
            {
                mainWindow.img.LayoutTransform = rt;
            }
        }

        /// <summary>
        /// Rotates left or right
        /// </summary>
        /// <param name="right"></param>
        // https://stackoverflow.com/a/62136307/13646636
        internal static void Rotate(bool right)
        {
            if (mainWindow.img.Source == null ||
                Properties.Settings.Default.PicGallery == 1 && GalleryFunctions.IsOpen)
            { return; }

            if (right)
            {
                Rotateint -= 90;
                if (Rotateint < 0) { Rotateint += 360; }
            }
            else
            {
                Rotateint += 90;
                if (Rotateint >= 360) { Rotateint -= 360; }
            }

            Rotate(Rotateint);
        }

        /// <summary>
        /// Flips the image
        /// </summary>
        internal static void Flip()
        {
            if (mainWindow.img.Source == null)
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

            rt.Angle = Rotateint;
            tg.Children.Add(flip);
            tg.Children.Add(rt);
            mainWindow.img.LayoutTransform = tg;
        }
    }
}