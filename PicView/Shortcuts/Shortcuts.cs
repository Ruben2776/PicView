using PicView.Native;
using System.Windows;
using System.Windows.Input;
using static PicView.Copy_Paste;
using static PicView.DeleteFiles;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.Helper;
using static PicView.Interface;
using static PicView.LoadWindows;
using static PicView.Navigation;
using static PicView.Open_Save;
using static PicView.Resize_and_Zoom;
using static PicView.Rotate_and_Flip;
using static PicView.SlideShow;
using static PicView.ToggleMenus;

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
                                return;
                        }
                    }
                    if (!e.IsRepeat)
                    {
                        // Go to first if Ctrl held down
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                            Pic(true, true);
                        else
                            Pic();
                    }
                    else if (canNavigate)
                    {
                        if (FolderIndex == Pics.Count - 1)
                            FolderIndex = 0;
                        else
                            FolderIndex++;

                        //fastPicTimer.Start();
                        FastPic();
                    }
                    break;

                // Prev
                case Key.BrowserBack:
                case Key.Left:
                case Key.A:
                    if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            if (Properties.Settings.Default.PicGallery == 1)
                                return;
                        }
                    }
                    if (!e.IsRepeat)
                    {
                        // Go to first if Ctrl held down
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                            Pic(false, true);
                        else
                            Pic(false);
                    }
                    else if (canNavigate)
                    {
                        if (FolderIndex == 0)
                            FolderIndex = Pics.Count - 1;
                        else
                            FolderIndex--;

                        //fastPicTimer.Start();
                        FastPic();
                    }
                    break;

                // Scroll
                case Key.PageUp:
                    if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            PicGalleryLogic.ScrollTo(true, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset - 30);
                    break;

                case Key.PageDown:
                    if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            PicGalleryLogic.ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + 30);
                    break;

                // Rotate or Scroll
                case Key.Up:
                case Key.W:
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                            return;
                        else
                            mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset - 30);
                    }
                    else if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            PicGalleryLogic.ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    else if (!e.IsRepeat && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                        Rotate(false);
                    break;
                case Key.Down:
                case Key.S:
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                            return; // Save Ctrl + S fix
                        else
                            mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + 30);
                    }
                    else if (picGallery != null)
                    {
                        if (PicGalleryLogic.IsOpen)
                        {
                            PicGalleryLogic.ScrollTo(true, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    else if (!e.IsRepeat && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                        Rotate(true);
                    break;

                // Zoom
                case Key.Add:
                case Key.OemPlus:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || IsScrollEnabled)
                        Zoom(1, true);
                    else
                        Zoom(1, false);
                    break;

                case Key.Subtract:
                case Key.OemMinus:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || IsScrollEnabled)
                        Zoom(-1, true);
                    else
                        Zoom(-1, false);
                    break;
            }
        }

        internal static void MainWindow_KeysUp(object sender, KeyEventArgs e)
        {
            #region Unused switch... Alt key won't work with switch
            //switch (e.Key)
            //{
            //    // FastPicUpdate()
            //    case Key.E:
            //    case Key.A:
            //    case Key.Right:
            //    case Key.D:
            //        if (!FastPicRunning)
            //            return;
            //        FastPicUpdate();
            //        break;
            //    // Esc
            //    case Key.Escape:
            //        if (UserControls_Open())
            //            Close_UserControls();
            //        else if (SlideshowActive)
            //            UnloadSlideshow();
            //        else if (picGallery != null)
            //            if (Properties.Settings.Default.PicGallery != 2 && PicGalleryLogic.IsOpen)
            //                PicGalleryLogic.PicGalleryFade(false);
            //            else
            //                SystemCommands.CloseWindow(mainWindow);
            //        else
            //            SystemCommands.CloseWindow(mainWindow);
            //        break;
            //    // Ctrl + Q
            //    case Key.Q:
            //        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //            SystemCommands.CloseWindow(mainWindow);
            //        break;
            //    // O, Ctrl + O
            //    case Key.O:
            //        Open();
            //        break;
            //    // X, Ctrl + X
            //    case Key.X:
            //        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //            Cut(PicPath);
            //        else
            //            IsScrollEnabled = IsScrollEnabled ? false : true;
            //        break;
            //    // F
            //    case Key.F:
            //        Flip();
            //        break;
            //    // Delete, Shift + Delete
            //    case Key.Delete:
            //        DeleteFile(PicPath, e.KeyboardDevice.Modifiers != ModifierKeys.Shift);
            //        break;
            //    // Ctrl + C, Ctrl + Shift + C
            //    case Key.C:
            //        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
            //            CopyBitmap();
            //        else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //            CopyPic();
            //        break;
            //    // Ctrl + V
            //    case Key.V:
            //        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //            Paste();
            //        break;
            //    // Ctrl + S
            //    case Key.S:
            //        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //            SaveFiles();
            //        break;
            //    // Ctrl + I
            //    case Key.I:
            //        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //            NativeMethods.ShowFileProperties(PicPath);
            //        break;
            //    // Ctrl + P
            //    case Key.P:
            //        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //            Print(PicPath);
            //        break;
            //    // Ctrl + R
            //    case Key.R:
            //        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //            Reload();
            //        break;
            //    // Alt + Enter
            //    case Key.Enter:
            //        if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Enter))
            //            mainWindow.Fullscreen_Restore();
            //        break;
            //    // Space
            //    case Key.Space:
            //        if (picGallery != null)
            //        {
            //            if (PicGalleryLogic.IsOpen)
            //            {
            //                PicGalleryLogic.ScrollTo();
            //                return;
            //            }
            //        }
            //        mainWindow.CenterWindowOnScreen();
            //        break;
            //    // F1
            //    case Key.F1:
            //        HelpWindow();
            //        break;
            //    // F2
            //    case Key.F2:
            //        AboutWindow();
            //        break;
            //    // F3
            //    case Key.F3:
            //        Open_In_Explorer();
            //        break;
            //    // F4
            //    case Key.F4:
            //        if (Properties.Settings.Default.PicGallery == 0)
            //            break;

            //        Properties.Settings.Default.PicGallery = 1;
            //        if (picGallery != null)
            //            PicGalleryLogic.PicGalleryFade(picGallery.Visibility == Visibility.Collapsed);
            //        break;
            //    // F5
            //    case Key.F5:
            //        if (Properties.Settings.Default.PicGallery == 0)
            //            return;

            //        var change = Properties.Settings.Default.PicGallery == 2;

            //        Properties.Settings.Default.PicGallery = 2;

            //        if (picGallery != null)
            //            PicGalleryLogic.PicGalleryFade(picGallery.Visibility == Visibility.Collapsed);

            //        if (change)
            //            Properties.Settings.Default.PicGallery = 1;
            //        break;
            //    // F6
            //    case Key.F6:
            //        ResetZoom();
            //        break;
            //    // F11
            //    case Key.F11:
            //        mainWindow.Fullscreen_Restore();
            //        break;
            //    // F12
            //    case Key.F12:
            //        if (!SlideshowActive)
            //            LoadSlideshow();
            //        else
            //            UnloadSlideshow();
            //        break;
            //    // Home
            //    case Key.Home:
            //        mainWindow.Scroller.ScrollToHome();
            //        break;
            //    // End
            //    case Key.End:
            //        mainWindow.Scroller.ScrollToEnd();
            //        break;
            //    // Alt + Z
            //    case Key.Z:
            //        if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Z))
            //            HideInterface();
            //        break;
            //}

            #endregion

            // FastPicUpdate()
            if (e.Key == Key.Left || e.Key == Key.A || e.Key == Key.Right || e.Key == Key.D)
            {
                if (!FastPicRunning)
                    return;
                FastPicUpdate();
            }

            // Esc
            else if (e.Key == Key.Escape)
            {
                if (UserControls_Open())
                {
                    Close_UserControls();
                }
                else if (SlideshowActive)
                {
                    UnloadSlideshow();
                }
                else if (picGallery != null)
                {
                    if (Properties.Settings.Default.PicGallery != 2 && PicGalleryLogic.IsOpen)
                        PicGalleryLogic.PicGalleryToggle();
                    else
                        SystemCommands.CloseWindow(mainWindow);
                }
                else
                {
                    SystemCommands.CloseWindow(mainWindow);
                }
            }

            // Ctrl + Q
            else if (e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SystemCommands.CloseWindow(mainWindow);
            }

            // O, Ctrl + O
            else if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || e.Key == Key.O)
            {
                Open();
            }

            // X
            else if (e.Key == Key.X)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    Cut(PicPath);
                else
                    IsScrollEnabled = IsScrollEnabled ? false : true;
            }

            // F
            else if (e.Key == Key.F)
            {
                Flip();
            }

            // Delete, Shift + Delete
            else if (e.Key == Key.Delete)
            {
                DeleteFile(PicPath, e.KeyboardDevice.Modifiers != ModifierKeys.Shift);
            }

            // Ctrl + C, Ctrl + Shift + C
            else if (e.Key == Key.C)
            {
                if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
                    CopyBitmap();
                else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    CopyPic();
            }

            // Ctrl + V
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Paste();
            }

            // Ctrl + S
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveFiles();
            }

            // Ctrl + I
            else if (e.Key == Key.I && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                NativeMethods.ShowFileProperties(PicPath);
            }

            // Ctrl + P
            else if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Print(PicPath);
            }

            // Ctrl + R
            else if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Reload();
            }

            // Alt + Enter
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Enter))
            {
                mainWindow.Fullscreen_Restore();
            }

            // Space
            else if (e.Key == Key.Space)
            {
                if (picGallery != null)
                {
                    if (PicGalleryLogic.IsOpen)
                    {
                        PicGalleryLogic.ScrollTo();
                        return;
                    }
                }
                mainWindow.CenterWindowOnScreen();
            }

            // F1
            else if (e.Key == Key.F1)
            {
                HelpWindow();
            }

            //F2
            else if (e.Key == Key.F2)
            {
                AboutWindow();
            }

            // F3
            else if (e.Key == Key.F3)
            {
                Open_In_Explorer();
            }

            // F4
            else if (e.Key == Key.F4)
            {
                if (picGallery != null)
                    PicGalleryLogic.PicGalleryToggle(Properties.Settings.Default.PicGallery == 2);
            }

            // F5
            else if (e.Key == Key.F5)
            {
                if (picGallery != null)
                    PicGalleryLogic.PicGalleryToggle(Properties.Settings.Default.PicGallery == 1);
            }

            // F6
            else if (e.Key == Key.F6)
            {
                ResetZoom();
            }

            //F10
            else if (e.Key == Key.F10)
            {
                mainWindow.Fullscreen_Restore();
            }

            //F12
            else if (e.Key == Key.F12)
            {
                if (!SlideshowActive)
                    LoadSlideshow();
                else
                    UnloadSlideshow();
            }

            // Home
            else if (e.Key == Key.Home)
            {
                mainWindow.Scroller.ScrollToHome();
            }

            // End
            else if (e.Key == Key.End)
            {
                mainWindow.Scroller.ScrollToEnd();
            }

            // Alt + Z
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Z))
            {
                HideInterface();
            }
        }

        internal static void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                case MouseButton.Left:
                    if (autoScrolling)
                        StopAutoScroll();
                    break;

                case MouseButton.Middle:
                    if (!autoScrolling)
                        StartAutoScroll(e);
                    else
                        StopAutoScroll();
                    break;

                case MouseButton.XButton1:
                    Pic(false);
                    break;

                case MouseButton.XButton2:
                    Pic();
                    break;

                default:
                    break;
            }
        }

        #endregion Keyboard & Mouse Shortcuts
    }
}
