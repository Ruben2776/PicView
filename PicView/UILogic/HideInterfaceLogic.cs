using PicView.ChangeImage;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic.Sizing;
using System.Windows;
using static PicView.Animations.FadeControls;
using Timer = System.Timers.Timer;

namespace PicView.UILogic;

internal static class HideInterfaceLogic
{
    /// <summary>
    /// Toggle between hidden interface and default
    /// </summary>
    internal static void ToggleInterface()
    {
        if (GalleryFunctions.IsHorizontalFullscreenOpen || Settings.Default.Fullscreen)
        {
            return;
        }

        if (Settings.Default.ShowInterface)
        {
            if (ConfigureWindows.GetMainWindow.TitleBar.Visibility == Visibility.Visible)
            {
                ShowMinimalInterface();
            }
            else
            {
                ShowStandardInterface();
            }
        }
        else
        {
            ShowStandardInterface();
        }

        UC.Close_UserControls();

        // Recalculate to new size
        var timer = new Timer(50) // If not fired in timer, calculation incorrect
        {
            AutoReset = false,
            Enabled = true,
        };
        timer.Elapsed += (_, _) => ScaleImage.TryFitImage();

        Settings.Default.Save();
    }

    internal static void ShowStandardInterface()
    {
        Settings.Default.ShowInterface = true;

        ShowTopAndBottom(true);
        ShowNavigation(false);
        ShowShortcuts(false);

        ActivityTimer?.Stop();
    }

    private static void ShowMinimalInterface()
    {
        ShowTopAndBottom(false);
        ShowNavigation(Settings.Default.ShowAltInterfaceButtons);
        ShowShortcuts(Settings.Default.ShowAltInterfaceButtons);

        Settings.Default.ShowInterface = false;

        ActivityTimer?.Start();
    }

    internal static void ShowTopAndBottom(bool show)
    {
        if (show)
        {
            ConfigureWindows.GetMainWindow.TitleBar.Visibility =
                ConfigureWindows.GetMainWindow.LowerBar.Visibility = Visibility.Visible;
        }
        else
        {
            ConfigureWindows.GetMainWindow.TitleBar.Visibility =
                ConfigureWindows.GetMainWindow.LowerBar.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Toggle alternative layout navigation
    /// </summary>
    /// <param name="show"></param>
    internal static void ShowNavigation(bool show)
    {
        if (UC.GetClickArrowLeft == null || UC.GetClickArrowRight == null
                                         || UC.GetX2 == null || UC.GetMinus == null || UC.GetRestoreButton == null)
        {
            return;
        }

        if (show)
        {
            if (ErrorHandling.CheckOutOfRange() is false)
            {
                UC.GetClickArrowLeft.Visibility =
                    UC.GetClickArrowRight.Visibility = Visibility.Visible;
            }

            UC.GetX2.Visibility =
                UC.GetRestoreButton.Visibility =
                    UC.GetMinus.Visibility = Visibility.Visible;
        }
        else
        {
            UC.GetClickArrowLeft.Visibility =
                UC.GetClickArrowRight.Visibility =
                    UC.GetX2.Visibility =
                        UC.GetRestoreButton.Visibility =
                            UC.GetMinus.Visibility = Visibility.Collapsed;
        }
    }

    internal static void ShowShortcuts(bool show)
    {
        if (UC.GetGalleryShortcut == null)
        {
            return;
        }

        if (show && !ErrorHandling.CheckOutOfRange())
        {
            UC.GetGalleryShortcut.Visibility = Visibility.Visible;
        }
        else
        {
            UC.GetGalleryShortcut.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Logic for mouse movements on MainWindow
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static void Interface_MouseMove()
    {
        Fade(true);
    }

    /// <summary>
    /// Logic for mouse leave mainwindow event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static void Interface_MouseLeave()
    {
        Fade(false);
    }
}