using PicView.ChangeImage;
using PicView.SystemIntegration;
using System.Threading.Tasks;
using System.Windows;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.UC;

namespace PicView.UILogic.Sizing
{
    internal static class WindowSizing
    {
        /// <summary>
        /// Used to get and set monitor size
        /// </summary>
        internal static MonitorSize MonitorInfo { get; set; }

        /// <summary>
        /// Set whether to fit window to image or image to window
        /// </summary>
        internal static bool SetWindowBehavior()
        {
            if (Properties.Settings.Default.AutoFitWindow)
            {
                ConfigureWindows.GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                ConfigureWindows.GetMainWindow.ResizeMode = ResizeMode.CanMinimize;
                ConfigureWindows.GetMainWindow.GripButton.Visibility = Visibility.Collapsed;

                if (GetQuickSettingsMenu != null)
                {
                    GetQuickSettingsMenu.SetFit.IsChecked = true;
                }

                ConfigureWindows.GetMainWindow.WindowState = WindowState.Normal;
            }
            else
            {
                ConfigureWindows.GetMainWindow.SizeToContent = SizeToContent.Manual;
                ConfigureWindows.GetMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;
                ConfigureWindows.GetMainWindow.GripButton.Visibility = Visibility.Visible;

                if (GetQuickSettingsMenu != null)
                {
                    GetQuickSettingsMenu.SetFit.IsChecked = false;
                }
            }

            return Properties.Settings.Default.AutoFitWindow;
        }
    }
}