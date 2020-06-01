using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace PicView
{
    internal static class Base64
    {
        public static BitmapImage Base64StringToBitmap(string base64String)
        {
            byte[] binaryData = Convert.FromBase64String(base64String);

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(binaryData);
            bi.EndInit();

            return bi;
        }

        internal static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
        }
    }
}
