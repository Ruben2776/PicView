using Avalonia.Media.Imaging;

namespace PicView.Data.Imaging
{
    public static class ImageDecoder
    {
        /// <summary>
        /// Decodes image from file to relevant image
        /// </summary>
        /// <param name="fileInfo">Cannot be null</param>
        /// <returns></returns>
        public static async Task<Avalonia.Media.IImage?> ReturnPicAsync(FileInfo fileInfo)
        {
            if (fileInfo == null) { return null; }
            if (fileInfo.Length <= 0) { return null; }

            switch (fileInfo.Extension)
            {
                case { } when fileInfo.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".jpe", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".gif", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".jfif", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".ico", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".webp", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".wbmp", StringComparison.OrdinalIgnoreCase):
                    return await Task.FromResult(GetBitmap(fileInfo)).ConfigureAwait(false);

                default:
                    return null;
            }
        }

        private static Bitmap? GetBitmap(FileInfo fileInfo)
        {
            try
            {
                if (fileInfo == null)
                {
                    throw new ArgumentNullException(nameof(fileInfo));
                }

                using var filestream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
                var image = new Bitmap(filestream);
                return image;
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Trace.WriteLine($"{nameof(GetBitmap)} {fileInfo?.Name} exception, \n {e.Message}");
#endif
                return null;
            }
        }
    }
}
