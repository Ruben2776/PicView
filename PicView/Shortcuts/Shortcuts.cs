using System.Windows;
using System.Windows.Input;
using static PicView.Copy_Paste;
using static PicView.DeleteFiles;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.GalleryScroll;
using static PicView.LoadWindows;
using static PicView.Navigation;
using static PicView.Open_Save;
using static PicView.Resize_and_Zoom;
using static PicView.Rotate_and_Flip;
using static PicView.Scroll;
using static PicView.ToggleGallery;
using static PicView.UC;
using static PicView.Utilities;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class Shortcuts
    {
        internal static void MainWindow_KeysDown(object sender, KeyEventArgs e)
        {
            // Don't allow keys when typing in text
            if (mainWindow.Bar.IsKeyboardFocusWithin) { return; }

            #region Keys where it can be held down

            switch (e.Key)
            {
                case Key.BrowserForward:
                case Key.Right:
                case Key.D:
                    if (picGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
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
                    else if (CanNavigate)
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
                        if (GalleryFunctions.IsOpen)
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
                    else if (CanNavigate)
                    {
                        FastPic(false);
                    }
                    return;

                // Scroll
                case Key.PageUp:
                    if (picGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
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
                        if (GalleryFunctions.IsOpen)
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
                        if (GalleryFunctions.IsOpen)
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
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        SaveFiles();
                    }
                    else if (Properties.Settings.Default.ScrollEnabled && mainWindow.Scroller.ScrollableHeight > 0)
                    {
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + 30);
                    }
                    else if (picGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
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

            #endregion Keys where it can be held down

            #region Key is not held down

            if (!e.IsRepeat)
            {
                switch (e.Key)
                {
                    // Esc
                    case Key.Escape:
                        if (UserControls_Open())
                        {
                            Close_UserControls();
                            return;
                        }
                        if (Properties.Settings.Default.Fullscreen)
                        {
                            if (SlideTimer != null)
                            {
                                if (SlideTimer.Enabled)
                                {
                                    SlideShow.StopSlideshow();
                                }
                            }
                            else
                            {
                                Fullscreen_Restore();
                            }
                            return;
                        }
                        if (GalleryFunctions.IsOpen)
                        {
                            Toggle();
                            return;
                        }
                        if (IsDialogOpen)
                        {
                            IsDialogOpen = false;
                            return;
                        }
                        if (infoWindow != null)
                        {
                            if (infoWindow.IsVisible)
                            {
                                infoWindow.Hide();
                                return;
                            }
                        }
                        if (allSettingsWindow != null)
                        {
                            if (allSettingsWindow.IsVisible)
                            {
                                allSettingsWindow.Hide();
                                return;
                            }
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
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                            {
                                Base64.SendToClipboard();
                            }
                            else if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                            {
                                CopyBitmap();
                            }
                            else
                            {
                                Copyfile();
                            }
                        }
                        else
                        {
                            // TODO add crop function
                        }
                        break;

                    // Ctrl + V
                    case Key.V:
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            Paste();
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
                        OpenWith(Pics[FolderIndex]);
                        break;

                    // T
                    case Key.T:
                        ChangeBackground(sender, e);
                        break;

                    // Space
                    case Key.Space:
                        if (picGallery != null)
                        {
                            if (GalleryFunctions.IsOpen)
                            {
                                ScrollTo();
                                return;
                            }
                        }
                        CenterWindowOnScreen();
                        break;

                    // 1
                    case Key.D1:
                        Tooltip.ShowTooltipMessage("Set to center image in window");
                        AutoFit = false;
                        break;

                    // 2
                    case Key.D2:
                        Tooltip.ShowTooltipMessage("Set to fit application to screen");
                        AutoFit = true;
                        break;

                    // F1
                    case Key.F1:
                        HelpWindow();
                        break;

                    // F2
                    case Key.F2:
                        EditTitleBar.EditTitleBar_Text();
                        break;

                    // F3
                    case Key.F3:
                        Open_In_Explorer();
                        break;

                    // F4
                    case Key.F4:
                        AllSettingsWindow();
                        break;

                    // F5
                    case Key.F5:
                        break;

                    // F6
                    case Key.F6:
                        ResetZoom();
                        break;

                    // F11
                    case Key.F11:
                        SlideShow.StartSlideshow();
                        break;

                    // F12
                    case Key.F12:
                        Fullscreen_Restore();
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

            #endregion Key is not held down

            #region Alt + keys

            // Alt doesn't work in switch? Waiting for key up is confusing in this case
            // Alt + Z
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Z))
            {
                if (!e.IsRepeat)
                {
                    HideInterfaceLogic.ToggleInterface();
                }
                //e.Handled = true;
            }

            // Alt + Enter
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Enter))
            {
                if (!e.IsRepeat)
                {
                    Fullscreen_Restore();
                }
                //e.Handled = true;
            }

            #endregion Alt + keys
        }

        internal static void MainWindow_KeysUp(object sender, KeyEventArgs e)
        {
            // Don't allow keys when typing in text
            if (mainWindow.Bar.IsKeyboardFocusWithin) { return; }

            switch (e.Key)
            {
                #region FastPicUpdate()
                case Key.A:
                case Key.Right:
                case Key.D:
                    FastPicUpdate();
                    break;
                    #endregion
            }
        }

        internal static void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                case MouseButton.Left:
                    if (AutoScrolling)
                    {
                        StopAutoScroll();
                    }

                    break;

                case MouseButton.Middle:
                    if (!AutoScrolling)
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

        internal static void CustomTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    EditTitleBar.HandleRename();
                    break;

                case Key.Escape:
                    EditTitleBar.Refocus();
                    IsDialogOpen = true; // Hack to make escape not fall through
                    break;
            }
        }
    }
}