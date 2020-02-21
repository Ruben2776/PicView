using System.Windows;
using System.Windows.Input;
using static PicView.Copy_Paste;
using static PicView.DeleteFiles;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.Helper;
using static PicView.HideInterfaceLogic;
using static PicView.LoadWindows;
using static PicView.Navigation;
using static PicView.Open_Save;
using static PicView.PicGalleryScroll;
using static PicView.PicGalleryToggle;
using static PicView.Resize_and_Zoom;
using static PicView.Rotate_and_Flip;
using static PicView.Scroll;
using static PicView.SlideShow;
using static PicView.ToggleMenus;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class Shortcuts
    {
        #region MainWindow Keyboard & Mouse Shortcuts

        internal static void MainWindow_KeysDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.BrowserForward:
                case Key.Right:
                case Key.D:
                    if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            if (Properties.Settings.Default.PicGallery == 1)
                            {
                                return;
                            }
                        }
                    }
                    if (!e.IsRepeat)
                    {
                        // Go to first if Ctrl held down
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            Pic(true, true);
                        }
                        else
                        {
                            Pic();
                        }
                    }
                    else if (canNavigate)
                    {
                        FastPic(true);
                    }
                    return;

                // Prev
                case Key.BrowserBack:
                case Key.Left:
                case Key.A:
                    if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            if (Properties.Settings.Default.PicGallery == 1)
                            {
                                return;
                            }
                        }
                    }
                    if (!e.IsRepeat)
                    {
                        // Go to last if Ctrl held down
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            Pic(false, true);
                        }
                        else
                        {
                            Pic(false);
                        }
                    }
                    else if (canNavigate)
                    {
                        FastPic(false);
                    }
                    return;

                // Scroll
                case Key.PageUp:
                    if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            ScrollTo(true, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset - 30);
                    }

                    return;

                case Key.PageDown:
                    if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + 30);
                    }

                    return;

                // Rotate or Scroll
                case Key.Up:
                case Key.W:
                    if (Properties.Settings.Default.ScrollEnabled && mainWindow.Scroller.ScrollableHeight > 0)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            return;
                        }
                        else
                        {
                            mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset - 30);
                        }
                    }
                    else if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                        }
                        else if (!e.IsRepeat && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                        {
                            Rotate(false);
                        }
                    }
                    else if (!e.IsRepeat && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                    {
                        Rotate(false);
                    }

                    return;
                case Key.Down:
                case Key.S:
                    if (Properties.Settings.Default.ScrollEnabled && mainWindow.Scroller.ScrollableHeight > 0)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            return; // Save Ctrl + S fix
                        }
                        else
                        {
                            mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + 30);
                        }
                    }
                    else if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            ScrollTo(true, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                        }
                        else if (!e.IsRepeat && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                        {
                            Rotate(true);
                        }
                    }
                    else if (!e.IsRepeat && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                    {
                        Rotate(true);
                    }

                    return;

                // Zoom
                case Key.Add:
                case Key.OemPlus:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || IsScrollEnabled)
                    {
                        Zoom(1, true);
                    }
                    else
                    {
                        Zoom(1, false);
                    }

                    return;

                case Key.Subtract:
                case Key.OemMinus:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || IsScrollEnabled)
                    {
                        Zoom(-1, true);
                    }
                    else
                    {
                        Zoom(-1, false);
                    }

                    return;
            }

            // Alt doesn't work in switch? Waiting for key up is confusing in this case
            // Alt + Z
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Z))
            {
                if (!e.IsRepeat)
                {
                    ToggleInterface();
                }

                return;
            }

            // Alt + Enter
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Enter))
            {
                if (!e.IsRepeat)
                {
                    Fullscreen_Restore();
                }
            }
        }

        internal static void MainWindow_KeysUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                // FastPicUpdate()
                case Key.A:
                case Key.Right:
                case Key.D:
                    FastPicUpdate();
                    break;
                // Esc
                case Key.Escape:
                    if (UserControls_Open())
                    {
                        Close_UserControls();
                        return;
                    }
                    if (SlideshowActive)
                    {
                        UnloadSlideshow();
                        return;
                    }
                    if (Properties.Settings.Default.Fullscreen)
                    {
                        Fullscreen_Restore();
                        return;
                    }
                    if (Properties.Settings.Default.PicGallery > 0)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            ToggleGallery();
                            return;
                        }
                    }
                    if (dialogOpen)
                    {
                        dialogOpen = false;
                        return;
                    }
                    if (!cm.IsVisible)
                    {
                        SystemCommands.CloseWindow(mainWindow);
                    }
                    break;
                // Ctrl + Q
                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        SystemCommands.CloseWindow(mainWindow);
                    }

                    break;
                // O, Ctrl + O
                case Key.O:
                    Open();
                    break;
                // X, Ctrl + X
                case Key.X:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Cut(Pics[FolderIndex]);
                    }
                    else
                    {
                        Configs.SetScrolling(sender, e);
                    }

                    break;
                // F
                case Key.F:
                    Flip();
                    break;
                // Delete, Shift + Delete
                case Key.Delete:
                    DeleteFile(Pics[FolderIndex], e.KeyboardDevice.Modifiers != ModifierKeys.Shift);
                    break;
                // Ctrl + C, Ctrl + Shift + C, Ctrl + Alt + C
                case Key.C:
                    // Ctrl + Shift + C
                    if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
                    {
                        CopyText();
                    }
                    // Ctrl + Alt + C
                    if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == (ModifierKeys.Control | ModifierKeys.Alt))
                    {
                        CopyBitmap();
                    }
                    // Ctrl + C
                    else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        CopyPic();
                    }
                    break;
                // Ctrl + V
                case Key.V:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Paste();
                    }

                    break;
                // Ctrl + S
                case Key.S:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        SaveFiles();
                    }

                    break;
                // Ctrl + I
                case Key.I:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        NativeMethods.ShowFileProperties(Pics[FolderIndex]);
                    }

                    break;
                // Ctrl + P
                case Key.P:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Print(Pics[FolderIndex]);
                    }

                    break;
                // Ctrl + R
                case Key.R:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Reload();
                    }

                    break;
                // L
                case Key.L:
                    Configs.SetLooping(sender, e);
                    break;
                // E
                case Key.E:
                    FitToWindow = FitToWindow ? false : true;
                    break;
                // T
                case Key.T:
                    ChangeBackground(sender, e);
                    break;
                // Space
                case Key.Space:
                    if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            ScrollTo();
                            return;
                        }
                    }
                    CenterWindowOnScreen();
                    break;
                // F1
                case Key.F1:
                    HelpWindow();
                    break;
                // F2
                case Key.F2:
                    AboutWindow();
                    break;
                // F3
                case Key.F3:
                    Open_In_Explorer();
                    break;
                // F4
                case Key.F4:
                    if (picGallery != null)
                    {
                        if (Properties.Settings.Default.PicGallery == 2)
                        {
                            ToggleGallery(true);
                        }
                        else
                        {
                            Properties.Settings.Default.PicGallery = 1;
                            ToggleGallery();
                        }
                    }
                    break;
                // F5
                case Key.F5:
                    if (picGallery != null)
                    {
                        if (Properties.Settings.Default.PicGallery == 1)
                        {
                            if (PicGalleryLogic.IsOpen)
                            {
                                ToggleGallery(true);
                            }
                            else
                            {
                                Properties.Settings.Default.PicGallery = 2;
                                ToggleGallery();
                            }
                        }
                        else
                        {
                            Properties.Settings.Default.PicGallery = 2;
                            ToggleGallery();
                        }
                    }
                    break;
                // F6
                case Key.F6:
                    ResetZoom();
                    break;
                // F11
                case Key.F11:
                    Fullscreen_Restore();
                    break;
                // F12
                case Key.F12:
                    if (!SlideshowActive)
                    {
                        LoadSlideshow();
                    }
                    else
                    {
                        UnloadSlideshow();
                    }

                    break;
                // Home
                case Key.Home:
                    mainWindow.Scroller.ScrollToHome();
                    break;
                // End
                case Key.End:
                    mainWindow.Scroller.ScrollToEnd();
                    break;
            }
        }

        internal static void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                case MouseButton.Left:
                    if (autoScrolling)
                    {
                        StopAutoScroll();
                    }

                    break;

                case MouseButton.Middle:
                    if (!autoScrolling)
                    {
                        StartAutoScroll(e);
                    }
                    else
                    {
                        StopAutoScroll();
                    }

                    break;

                case MouseButton.XButton1:
                    Pic(false);
                    break;

                case MouseButton.XButton2:
                    Pic();
                    break;
            }
        }

        #endregion Keyboard & Mouse Shortcuts
    }
}
