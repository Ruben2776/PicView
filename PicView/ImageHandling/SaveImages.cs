using ImageMagick;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using static PicView.UI.TransformImage.Rotation;

namespace PicView.ImageHandling
{
    internal static class SaveImages
    {
        /// <summary>
        /// Tries to save image to the specified destination,
        /// returns false if unsuccesful
        /// </summary>
        /// <param name="rotate">Degrees image is rotated by</param>
        /// <param name="flipped">Whether to flip image or not</param>
        /// <param name="path">The path of the source file</param>
        /// <param name="destination">Where to save image to</param>
        /// <returns></returns>
        internal static bool TrySaveImage(int rotate, bool flipped, string path, string destination)
        {
            try
            {
                using var SaveImage = new MagickImage();
                // Set maximum quality
                var mrs = new MagickReadSettings()
                {
                    Density = new Density(300, 300),
                };
                SaveImage.Quality = 100;

                SaveImage.Read(path, mrs);

                // Apply transformation values
                if (flipped)
                {
                    SaveImage.Flop();
                }

                SaveImage.Rotate(rotate);

                SaveImage.Write(destination);
            }
            catch (Exception) { return false; }
            return true;
        }

        internal static bool TrySaveImage(int rotate, bool flipped, BitmapSource bitmapSource, string destination)
        {
            try
            {
                using var SaveImage = new MagickImage();
                var encoder = new PngBitmapEncoder(); // or any other BitmapEncoder

                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    SaveImage.Read(stream.ToArray());
                }

                SaveImage.Quality = 100;

                // Apply transformation values
                if (flipped)
                {
                    SaveImage.Flop();
                }

                SaveImage.Rotate(rotate);

                SaveImage.Write(destination);
            }
            catch (Exception) { return false; }
            return true;
        }

        /// <summary>
        /// Tries to save image to the specified destination,
        /// returns false if unsuccesful
        /// </summary>
        /// <param name="croppedBitmap">The cropped bitmapy</param>
        /// /// <param name="destination">Where to save image to</param>
        /// <returns></returns>
        internal static bool TrySaveImage(Int32Rect rect, string path, string destination)
        {
            try
            {
                using var SaveImage = new MagickImage
                {
                    Quality = 100
                };

                SaveImage.Read(path);

                if (Rotateint != 0)
                {
                    SaveImage.Rotate(Rotateint);
                }

                SaveImage.Crop(new MagickGeometry(rect.X, rect.Y, rect.Width, rect.Height));

                SaveImage.Write(destination);
            }
            catch (Exception) { return false; }
            return true;
        }

        internal static bool TrySaveImage(Int32Rect rect, BitmapSource bitmapSource, string destination)
        {
            try
            {
                using var SaveImage = new MagickImage
                {
                    Quality = 100
                };

                var encoder = new PngBitmapEncoder(); // or any other BitmapEncoder

                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    SaveImage.Read(stream.ToArray());
                }

                if (Rotateint != 0)
                {
                    SaveImage.Rotate(Rotateint);
                }

                SaveImage.Crop(new MagickGeometry(rect.X, rect.Y, rect.Width, rect.Height));

                SaveImage.Write(destination);
            }
            catch (Exception) { return false; }
            return true;
        }

        internal static void ResizeImageToFile(string file, int NewWidth, int NewHeight)
        {
            try
            {
                using (MagickImage magick = new MagickImage())
                {
                    MagickGeometry size = new MagickGeometry(NewWidth, NewHeight)
                    {
                        IgnoreAspectRatio = true
                    };
                    magick.Resize(size);
                    magick.Quality = 100;
                    magick.Write(file);
                }
            }
#if DEBUG
            catch (MagickException e)
            {
                Trace.WriteLine("ResizeImageToFile " + file + " null, \n" + e.Message);
                return;
            }
#else
                catch (MagickException) { return; }
#endif
        }
    }
}