using System.Windows.Media;
using static PicView.Fields;

namespace PicView
{
    internal static class Rotate_and_Flip
    {
        /// <summary>
        /// Rotates the image the specified degrees and updates imageSettingsMenu value
        /// </summary>
        /// <param name="r"></param>
        internal static void Rotate(int r)
        {
            if (mainWindow.img.Source == null)
            {
                return;
            }

            var rt = new RotateTransform { Angle = Rotateint = r };

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

            switch (r)
            {
                case 0:
                    imageSettingsMenu.Rotation0Button.IsChecked = true;
                    imageSettingsMenu.Rotation90Button.IsChecked = false;
                    imageSettingsMenu.Rotation180Button.IsChecked = false;
                    imageSettingsMenu.Rotation270Button.IsChecked = false;
                    break;

                case 180:
                    imageSettingsMenu.Rotation180Button.IsChecked = true;
                    imageSettingsMenu.Rotation90Button.IsChecked = false;
                    imageSettingsMenu.Rotation0Button.IsChecked = false;
                    imageSettingsMenu.Rotation270Button.IsChecked = false;
                    break;

                case 90:
                    imageSettingsMenu.Rotation90Button.IsChecked = true;
                    imageSettingsMenu.Rotation0Button.IsChecked = false;
                    imageSettingsMenu.Rotation180Button.IsChecked = false;
                    imageSettingsMenu.Rotation270Button.IsChecked = false;
                    break;

                case 270:
                    imageSettingsMenu.Rotation270Button.IsChecked = true;
                    imageSettingsMenu.Rotation90Button.IsChecked = false;
                    imageSettingsMenu.Rotation180Button.IsChecked = false;
                    imageSettingsMenu.Rotation0Button.IsChecked = false;
                    break;

                default:
                    imageSettingsMenu.Rotation0Button.IsChecked = true;
                    break;
            }

            // TODO Make a way to respect monitor height at 90 and 270 degrees
        }

        /// <summary>
        /// Rotates left or right
        /// </summary>
        /// <param name="right"></param>
        internal static void Rotate(bool right)
        {
            if (mainWindow.img.Source == null || PicGalleryLogic.IsOpen)
            {
                return;
            }

            switch (Rotateint)
            {
                case 0:
                    if (right)
                    {
                        Rotate(270);
                    }
                    else
                    {
                        Rotate(90);
                    }

                    break;

                case 90:
                    if (right)
                    {
                        Rotate(0);
                    }
                    else
                    {
                        Rotate(180);
                    }

                    break;

                case 180:
                    if (right)
                    {
                        Rotate(90);
                    }
                    else
                    {
                        Rotate(270);
                    }

                    break;

                case 270:
                    if (right)
                    {
                        Rotate(180);
                    }
                    else
                    {
                        Rotate(0);
                    }

                    break;
            }
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
