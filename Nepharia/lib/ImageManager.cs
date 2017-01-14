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
        #region Error Handling

        static bool Supported(string file, string extension)
        {
            extension = extension.Substring(1);
            MagickFormat format;
            if (!Enum.TryParse<MagickFormat>(extension, true, out format))
                return false;

            return (from formatInfo in MagickNET.SupportedFormats
                    where formatInfo.IsReadable && formatInfo.Format == format
                    select formatInfo).Any();
        }

        #endregion

        #region RenderToBitmapSource

        internal static BitmapSource RenderToBitmapSource(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || file.Length < 2)
                return null;

            string extension = Path.GetExtension(file);
            if (string.IsNullOrEmpty(extension))
                return null;

            extension.ToLowerInvariant();

            if (extension == ".gif")
                return GetBitmapImage(new Uri(file));
            else if (!Supported(file, extension))
                return null;

            BitmapSource pic;
            try
            {

                using (MagickImage magick = new MagickImage(file))
                {
                    magick.Read(file);
                    magick.Quality = 100;
                    magick.ColorSpace = ColorSpace.Transparent;
                    /*
                    if (magick.Width > SystemParameters.PrimaryScreenWidth + 500 || magick.Height > SystemParameters.PrimaryScreenHeight + 500)
                        //magick.Resize((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
                        magick.Thumbnail((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
                    */
                    if (extension == ".svg")
                    {
                        var mrs = new MagickReadSettings();
                        mrs.Format = MagickFormat.Svg;
                        mrs.ColorSpace = ColorSpace.Transparent;
                        mrs.BackgroundColor = MagickColors.Transparent;
                        magick.Read(file, mrs);
                        magick.Format = MagickFormat.Png;
                        var stream = new MemoryStream();
                        magick.Write(stream);
                        pic = GetBitmapImage(stream);
                    }
                    else
                        pic = magick.ToBitmapSource();
                    pic.Freeze();
                    return pic;
                }
            }
            catch (Exception)
            {
                return null;
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
