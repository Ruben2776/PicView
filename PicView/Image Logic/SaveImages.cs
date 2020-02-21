using ImageMagick;
using System.Diagnostics;

namespace PicView
{
    internal static class SaveImages
    {
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
