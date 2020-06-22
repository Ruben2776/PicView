using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace PicView.Library
{
    internal static class Utilities
    {
        #region static helpers

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

        #endregion static helpers

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
    }
}