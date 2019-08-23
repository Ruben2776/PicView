using System.Windows;
using static PicView.Fields;
using static PicView.Resize_and_Zoom;

namespace PicView
{
    internal static class ResizeLogic
    {
        /// <summary>
        /// Set whether to fit window to image or image to window
        /// </summary>
        internal static bool FitToWindow
        {
            get
            {
                return Properties.Settings.Default.FitToWindow;
            }
            set
            {
                Properties.Settings.Default.FitToWindow = value;

                if (value)
                {
                    mainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    mainWindow.ResizeMode = ResizeMode.NoResize;

                    if (quickSettingsMenu != null)
                        quickSettingsMenu.SetFit.IsChecked = true;

                    mainWindow.WindowState = WindowState.Normal;

                }
                else
                {
                    mainWindow.SizeToContent = SizeToContent.Manual;
                    mainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    if (quickSettingsMenu != null)
                        quickSettingsMenu.SetCenter.IsChecked = true;
                }

                if (mainWindow.img.Source != null)
                    ZoomFit(mainWindow.img.Source.Width, mainWindow.img.Source.Height);            }
        }
    }
}
