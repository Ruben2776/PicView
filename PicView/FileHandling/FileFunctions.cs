using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace PicView.FileHandling
{
    internal static class FileFunctions
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
        internal static string GetSizeReadable(long i)
        {
            string sign = i < 0 ? "-" : string.Empty;
            char prefix;
            double value;

            if (i >= 0x40000000) // Gigabyte
            {
                prefix = 'G';
                value = i >> 20;
            }
            else if (i >= 0x100000) // Megabyte
            {
                prefix = 'M';
                value = i >> 10;
            }
            else if (i >= 0x400) // Kilobyte
            {
                prefix = 'K';
                value = i;
            }
            else
            {
                return i.ToString(sign + "0 B", CultureInfo.CurrentCulture); // Byte
            }
            value /= 1024;

            return sign + value.ToString("0.## ", CultureInfo.CurrentCulture) + prefix + 'B';
        }

        internal static bool FilePathHasInvalidChars(string path)
        {
            return !string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;
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

        internal static string GetDefaultExeConfigPath(ConfigurationUserLevel userLevel)
        {
            try
            {
                var UserConfig = ConfigurationManager.OpenExeConfiguration(userLevel);
                return UserConfig.FilePath;
            }
            catch (ConfigurationException e)
            {
                return e.Filename;
            }
        }

        internal static string GetWritingPath()
        {
            return Path.GetDirectoryName(GetDefaultExeConfigPath(ConfigurationUserLevel.PerUserRoamingAndLocal));
        }

        internal static string GetURL(string value)
        {
            try
            {
                var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                return linkParser.Match(value).ToString();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                return string.Empty;
            }
        }

    }
}