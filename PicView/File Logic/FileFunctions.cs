using System;
using System.IO;
using System.Text.RegularExpressions;

namespace PicView
{
    class FileFunctions
    {

        internal static bool RenameFile(string PicPath, string PicNewPath)
        {
            if (File.Exists(PicNewPath))
                return false;

            try
            {
                File.Move(PicPath, PicNewPath);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Return file size in a readable format
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        /// Credits to http://www.somacon.com/p576.php
        internal static string GetSizeReadable(long i)
        {
            string sign = (i < 0 ? "-" : string.Empty);
            double readable = (i < 0 ? -i : i);
            char suffix;

            if (i >= 0x40000000) // Gigabyte
            {
                suffix = 'G';
                readable = (i >> 20);
            }
            else if (i >= 0x100000) // Megabyte
            {
                suffix = 'M';
                readable = (i >> 10);
            }
            else if (i >= 0x400) // Kilobyte
            {
                suffix = 'K';
                readable = i;
            }
            else
            {
                return i.ToString(sign + "0 B"); // Byte
            }
            readable /= 1024;

            return sign + readable.ToString("0.## ") + suffix + 'B';
        }

        

        #region File Methods

        internal static bool FilePathHasInvalidChars(string path)
        {
            return (!string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0);
        }

        internal static string MakeValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }

        #endregion File Methods
    }

   
}
