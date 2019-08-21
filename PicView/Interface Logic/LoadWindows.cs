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
                Width = mainWindow.Width,
                Height = mainWindow.Height,
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
            Window window = new Help
            {
                Width = mainWindow.Width,
                Height = mainWindow.Height,
                Opacity = 0,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(UIElement.OpacityProperty, animation);
            window.Show();
        }

        /// <summary>
        /// Show All Settings window in a dialog
        /// </summary>
        internal static void AllSettingsWindow()
        {
            Window window = new AllSettings
            {
                Width = mainWindow.Width,
                Height = mainWindow.Height,
                Opacity = 0,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(UIElement.OpacityProperty, animation);

            window.ShowDialog();
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
