using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace PicView.Library
{
    internal static class Utilities
    {

        /// <summary>
        /// Greatest Common Divisor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal static int GCD(int x, int y)
        {
            return y == 0 ? x : GCD(y, x % y);
        }

        /// <summary>
        /// Gets the absolute mouse position, relative to screen
        /// </summary>
        /// <returns></returns>
        internal static Point GetMousePos(UIElement element)
        {
            return element.PointToScreen(Mouse.GetPosition(element));
        }

        public static string GetDefaultExeConfigPath(ConfigurationUserLevel userLevel)
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

        public static string GetWritingPath()
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

        internal static string HexConverter(System.Drawing.Color c)
        {
            return "#" +
                c.R.ToString("X2", CultureInfo.InvariantCulture) +
                c.G.ToString("X2", CultureInfo.InvariantCulture) +
                c.B.ToString("X2", CultureInfo.InvariantCulture);
        }

        internal static string RGBConverter(System.Drawing.Color c)
        {
            return "RGB(" +
                c.R.ToString("X2", CultureInfo.InvariantCulture) +
                c.G.ToString("X2", CultureInfo.InvariantCulture) +
                c.B.ToString("X2", CultureInfo.InvariantCulture) + ")";
        }
    }
}