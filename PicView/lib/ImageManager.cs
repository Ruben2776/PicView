using ImageMagick;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace PicView.lib
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

            using (MagickImage magick = new MagickImage())
            {                    
                magick.Quality = 100;
                magick.ColorSpace = ColorSpace.Transparent;

                var mrs = new MagickReadSettings() {
                    Density = new Density(300),
                    CompressionMethod = CompressionMethod.NoCompression
                };

                if (extension == ".svg")
                {
                    //Make background transparent
                    mrs.Format = MagickFormat.Svg;
                    mrs.BackgroundColor = MagickColors.Transparent;
                    magick.Read(file, mrs);                    
                }
                else
                {
                    magick.Read(file);
                }

                pic = magick.ToBitmapSource();
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

        #endregion

        #region GetMagickImage(Stream s)
        internal static BitmapSource GetMagickImage(Stream s)
        {
            BitmapSource pic;

            using (MagickImage magick = new MagickImage())
            {
                magick.Quality = 100;
                var mrs = new MagickReadSettings();
                mrs.Density = new Density(300);

                magick.Read(s);
                magick.ColorSpace = ColorSpace.Transparent;
                pic = magick.ToBitmapSource();

                pic.Freeze();
                return pic;
            }
        }
        #endregion

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
