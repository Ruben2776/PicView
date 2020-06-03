using PicView.Windows;
using System.Diagnostics;
using System.Windows;
using static PicView.Fields;

namespace PicView
{
    internal static class LoadWindows
    {
        #region Windows

        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
        internal static void HelpWindow()
        {
            if (infoWindow == null)
            {
                infoWindow = new Info
                {
                    Owner = mainWindow
                };

                infoWindow.Show();
            }
            else
            {
                if (infoWindow.Visibility == Visibility.Visible)
                {
                    infoWindow.Focus();
                }
                else
                {
                    infoWindow.Show();
                }
            }

#if DEBUG
            Trace.WriteLine("HelpWindow loaded ");
#endif
        }

        /// <summary>
        /// Show All Settings window
        /// </summary>
        internal static void AllSettingsWindow()
        {
            if (allSettingsWindow == null)
            {
                allSettingsWindow = new AllSettings
                {
                    Owner = mainWindow
                };

                allSettingsWindow.Show();
            }
            else
            {
                if (allSettingsWindow.Visibility == Visibility.Visible)
                {
                    allSettingsWindow.Focus();
                }
                else
                {
                    allSettingsWindow.Show();
                }
            }

#if DEBUG
            Trace.WriteLine("HelpWindow loaded ");
#endif
        }

        ///// <summary>
        ///// Show Tools window
        ///// </summary>
        //internal static void ToolsWindow()
        //{
        //    new Tools().Show();
        //}

        #endregion Windows
    }
}
