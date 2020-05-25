using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace PicView
{
    class FileFunctions
    {

        internal static bool RenameFile(string path, string newPath)
        {
            try
            {
                File.Move(path, newPath);
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                return false;
            }
            return true;
        }



        /// <summary>
        /// Returns the human-readable file size for an arbitrary, 64-bit file size 
        /// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        /// </summary>
        /// <param name="i">FileInfo.Length</param>
        /// <returns></returns>
        /// Credits to http://www.somacon.com/p576.php
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        internal static string GetSizeReadable(long i)
        {
            string sign = (i < 0 ? "-" : string.Empty);
            double readable = i < 0 ? -i : i;
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
                return i.ToString(sign + "0 B", CultureInfo.CurrentCulture); // Byte
            }
            readable /= 1024;

            return sign + readable.ToString("0.## ", CultureInfo.CurrentCulture) + suffix + 'B';
        }


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

        internal static string Shorten(string name, int amount)
        {
            if (name.Length >= 25)
            {
                name = name.Substring(0, amount);
                name += "...";
            }
            return name;
        }
    }


}
