using ImageMagick;
using System.Globalization;
using System.IO;
using System.Windows;

namespace PicView.ImageHandling
{
    internal static class GetImageData
    {
        internal static string[] RetrieveData(string file)
        {
            var fileInfo = new FileInfo(file);
            using var image = new MagickImage(file);

            return new string[]
            {
                fileInfo.Name,
                fileInfo.DirectoryName,
                fileInfo.FullName,
                fileInfo.CreationTime.ToString(CultureInfo.CurrentCulture),
                fileInfo.LastWriteTime.ToString(CultureInfo.CurrentCulture),
                image.Width + " x " + image.Height + " " + Application.Current.Resources["Pixels"],
                image.Density.X + " x " + image.Density.Y + " " + Application.Current.Resources["Dpi"],
                (image.Depth * 3).ToString(CultureInfo.CurrentCulture),
            };
        }
    }
}
