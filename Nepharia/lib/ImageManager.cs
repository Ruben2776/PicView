using ImageMagick;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Nepharia.lib
{
    internal static class ImageManager
    {
        #region RenderToBitmapSource

        internal static BitmapSource RenderToBitmapSource(string file, string extension)
        {
            if (string.IsNullOrWhiteSpace(file) || file.Length < 2)
                return null;

            if (extension == ".gif")
                return GetBitmapImage(new Uri(file));

            BitmapSource pic;

            using (MagickImage magick = new MagickImage(file))
            {                    
                magick.Quality = 100;

                if (extension == ".svg")
                {
                    //Create settings and make background transparent
                    var mrs = new MagickReadSettings();
                    mrs.Format = MagickFormat.Svg;
                    mrs.ColorSpace = ColorSpace.Transparent;
                    mrs.BackgroundColor = MagickColors.Transparent;

                    //Write to stream as png
                    magick.Read(file, mrs);
                    magick.Format = MagickFormat.Png;
                    var stream = new MemoryStream();
                    magick.Write(stream);
                    pic = GetBitmapImage(stream);
                }
                else
                {
                    magick.Read(file);
                    magick.ColorSpace = ColorSpace.Transparent;
                    pic = magick.ToBitmapSource();
                }
                    
                pic.Freeze();
                return pic;
            }

        }

        #endregion

        #region GetBitmapImage

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
                var failpic = new BitmapImage();
                failpic.BeginInit();
                failpic.UriSource = s;
                failpic.CacheOption = BitmapCacheOption.None;
                failpic.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                failpic.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                /// Some images crash without theese settings :(
                failpic.EndInit();
                failpic.Freeze();
                return failpic;
            }
            pic.Freeze();
            return pic;
        }

        internal static BitmapImage GetBitmapImage(Stream s)
        {
            var pic = new BitmapImage();
            pic.BeginInit();
            pic.StreamSource = s;
            pic.CacheOption = BitmapCacheOption.OnLoad;
            try
            {
                pic.EndInit();
            }
            catch (ArgumentException)
            {
                var failpic = new BitmapImage();
                failpic.BeginInit();
                failpic.StreamSource = s;
                failpic.CacheOption = BitmapCacheOption.OnLoad;
                failpic.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                failpic.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                /// Some images crash without theese settings :(
                failpic.EndInit();
                failpic.Freeze();
                return failpic;
            }
            pic.Freeze();
            return pic;
        }

        #endregion

        internal static BitmapSource GetMagickImage(Stream s)
        {
            BitmapSource pic;
            try
            {
                using (var magick = new MagickImage())
                {
                    magick.Read(s);
                    magick.Quality = 100;
                    pic = magick.ToBitmapSource();
                }
                pic.Freeze();
                return pic;
            }
            catch (Exception)
            {
                return null;
            }
        }

        //internal static BitmapSource GetMagickImage(String s, int width, int height)
        //{
        //    BitmapSource pic;
        //    try
        //    {
        //        using (var magick = new MagickImage())
        //        {
        //            magick.Read(s);
        //            magick.Quality = 60;
        //            magick.AdaptiveResize(width, height);
        //            pic = magick.ToBitmapSource();
        //        }
        //        pic.Freeze();
        //        return pic;
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}

    }
}
