using PicView.PicGallery;
using PicView.UILogic.Sizing;
using System.Windows.Media;

namespace PicView.UILogic.TransformImage
{
    internal static class Rotation
    {
        internal static bool Flipped { get; set; }

        /// <summary>
        /// Used to get and set image rotation by degrees
        /// </summary>
        internal static int Rotateint { get; set; }

        /// <summary>
        /// Rotates the image the specified degrees
        /// </summary>
        /// <param name="r"></param>
        internal static void Rotate(int r)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null)
            {
                return;
            }

            var rt = new RotateTransform { Angle = Rotateint = r };

            _ = ScaleImage.TryFitImageAsync();

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
        /// Rotates left or right
        /// </summary>
        /// <param name="right"></param>
        internal static void Rotate(bool right)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null ||
                Properties.Settings.Default.FullscreenGalleryHorizontal == false && GalleryFunctions.IsHorizontalOpen)
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

            rt.Angle = Rotateint;
            tg.Children.Add(flip);
            tg.Children.Add(rt);
            ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = tg;
        }
    }
}