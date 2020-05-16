using PicView.Windows;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using static PicView.Fields;

namespace PicView
{
    internal static class LoadWindows
    {
        #region Windows

        /// <summary>
        /// Show About window in a dialog
        /// </summary>
        internal static void AboutWindow()
        {
            Window window = new About
            {
                Width = double.IsNaN(mainWindow.Width) ? 465 : mainWindow.Width,
                Height = double.IsNaN(mainWindow.Height) ? 715 : mainWindow.Height,
                Opacity = 0,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(UIElement.OpacityProperty, animation);

            window.ShowDialog();
        }

        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
        internal static void HelpWindow()
        {
            if (help == null)
            {
                help = new Info
                {
                    Owner = mainWindow
                };

                help.Show();
            }
            else
            {
                if (help.Visibility == Visibility.Visible)
                {
                    help.Focus();
                }
                else
                {
                    help.Show();
                }

            }
        }

        /// <summary>
        /// Show All Settings window
        /// </summary>
        internal static void AllSettingsWindow()
        {
            if (allSettings == null)
            {
                allSettings = new AllSettings
                {
                    Owner = mainWindow
                };

                allSettings.Show();
            }
            else
            {
                if (allSettings.Visibility == Visibility.Visible)
                {
                    allSettings.Focus();
                }
                else
                {
                    allSettings.Show();
                }
                
            }
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
