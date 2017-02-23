using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace PicView.lib
{
    internal static class ImageManager
    {
        internal static BitmapSource RenderToBitmapSource(string file, string extension)
        {
            if (string.IsNullOrWhiteSpace(file) || file.Length < 2)
                return null;

            if (extension == ".ico")
                return GetBitmapImage(new Uri(file));

            BitmapSource pic;

            using (MagickImage magick = new MagickImage())
            {
                magick.Quality = 100;
                magick.ColorSpace = ColorSpace.Transparent;

                var mrs = new MagickReadSettings()
                {
                    Density = new Density(300, 300),
                    CompressionMethod = CompressionMethod.NoCompression
                };

                if (extension == ".svg")
                {
                    // Make background transparent
                    mrs.Format = MagickFormat.Svg;
                    mrs.BackgroundColor = MagickColors.Transparent;
                    try
                    {
                        magick.Read(file, mrs);
                    }
                    catch (MagickException)
                    {
                        return null;
                    }
                }
                else
                {
                    try
                    {
                        magick.Read(file);
                    }
                    catch (MagickException)
                    {
                        return null;
                    }
                }

                pic = magick.ToBitmapSource();
                pic.Freeze();
                return pic;
            }

        }

        internal static BitmapImage GetBitmapImage(Uri s)
        {
            var pic = new BitmapImage();
            pic.BeginInit();
            pic.UriSource = s;
            pic.CacheOption = BitmapCacheOption.None;
            try
            {
                pic.EndInit();
            }
            catch (ArgumentException)
            {
                // Some images crash without these settings
                var failpic = new BitmapImage();
                failpic.BeginInit();
                failpic.UriSource = s;
                failpic.CacheOption = BitmapCacheOption.None;
                failpic.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                failpic.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                failpic.EndInit();
                failpic.Freeze();
                return failpic;
            }
            pic.Freeze();
            return pic;
        }

        internal static BitmapSource GetMagickImage(Stream s)
        {
            BitmapSource pic;

            using (MagickImage magick = new MagickImage())
            {
                magick.Quality = 100;
                var mrs = new MagickReadSettings()
                {
                    Density = new Density(300)
                };

                magick.Read(s);
                magick.ColorSpace = ColorSpace.Transparent;
                pic = magick.ToBitmapSource();

                pic.Freeze();
                return pic;
            }
        }

        internal static bool TrySaveImage(int rotate, bool flipped, string path, string destination)
        {
            if (File.Exists(path))
            {
                try
                {
                    MagickImage SaveImage = new MagickImage(path);

                    if (flipped)
                    {
                        SaveImage.Flop();
                    }

                    SaveImage.Rotate(rotate);
                    SaveImage.Write(destination);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {       
                return false;
            }           
            
            return true;
        }


        internal static void DeleteFile(string fileName)
        {
            FileSystem.DeleteFile(fileName, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
        }


        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="isMoveToRecycleBin">True: Move to Recycle bin | False: Delete permanently</param>
        /// <returns></returns>
        internal static void DeleteFile(string fileName, bool isMoveToRecycleBin)
        {
            if (isMoveToRecycleBin)
            {
                FileSystem.DeleteFile(fileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            else
            {
                DeleteFile(fileName);
            }
        }


    }
}
