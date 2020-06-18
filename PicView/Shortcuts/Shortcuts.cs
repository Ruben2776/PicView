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
using static PicView.Pan_and_Zoom;
using static PicView.Rotate_and_Flip;
using static PicView.Scroll;
using static PicView.GalleryToggle;
using static PicView.UC;
using static PicView.Utilities;
using static PicView.WindowLogic;
using System.IO;
using System;

namespace PicView
{
    internal static class Shortcuts
    {
        internal static void MainWindow_KeysDown(object sender, KeyEventArgs e)
        {
            // Don't allow keys when typing in text
            if (mainWindow.Bar.IsKeyboardFocusWithin) { return; }

            var ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            var altDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            var shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            #region CroppingKeys

            if (cropppingTool != null)
            {
                if (cropppingTool.IsVisible)
                {
                    if (e.Key == Key.Escape)
                    {
                        if (Pics.Count == 0)
                        {
                            SetTitle.SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, "Custom image");
                        }
                        else
                        {
                            SetTitle.SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, FolderIndex);
                        }
                        mainWindow.bg.Children.Remove(cropppingTool);
                        return;
                    }

                    if (e.Key == Key.Enter)
                    {
                        if (Pics.Count == 0)
                        {
                            SetTitle.SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, "Custom image");
                        }
                        else
                        {
                            SetTitle.SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, FolderIndex);
                        }
                        
                        ImageCropping.SaveCrop();
                        mainWindow.bg.Children.Remove(cropppingTool);
                    }

                    if (shiftDown)
                    {
                        // TODO keep it square when shift held down
                    }
                }
            }

            #endregion

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
                        if (ctrlDown)
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
                        if (ctrlDown)
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

                case Key.PageUp:
                    if (picGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            ScrollTo(true, ctrlDown);
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

                case Key.Up:
                case Key.W:
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        if (ctrlDown)
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
                            if (Properties.Settings.Default.PicGallery == 1)
                            {
                                ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                                return;
                            }
                            if (ctrlDown) 
                            {
                                Rotate(false);
                            }
                            else
                            {
                                Pic(false);
                            }
                        }
                        else
                        {
                            Rotate(false);
                        }
                    }
                    else
                    {
                        Rotate(false);
                    }

                    return;

                case Key.Down:
                case Key.S:
                    if (ctrlDown && picGallery != null && !GalleryFunctions.IsOpen)
                    {
                        SaveFiles();
                    }
                    else if (Properties.Settings.Default.ScrollEnabled)
                    {
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + 30);
                    }
                    else if (picGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            if (Properties.Settings.Default.PicGallery == 1)
                            {
                                ScrollTo(true, ctrlDown);
                                return;
                            }
                            if (ctrlDown)
                            {
                                Rotate(true);
                            }
                            else
                            {
                                Pic();
                            }
                        }
                        else
                        {
                            Rotate(true);
                        }
                    }
                    else
                    {
                        Rotate(true);
                    }

                    return;

                // Zoom
                case Key.Add:
                case Key.OemPlus:
                        Zoom(1, ctrlDown);
                    return;

                case Key.Subtract:
                case Key.OemMinus:
                        Zoom(-1, ctrlDown);
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
                        if (ctrlDown)
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
                        if (ctrlDown)
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
                        DeleteFile(Pics[FolderIndex], !shiftDown);
                        break;

                    // Ctrl + C, Ctrl + Shift + C, Ctrl + Alt + C
                    case Key.C:
                        if (ctrlDown)
                        {
                            if (resizeAndOptimize != null)
                            {
                                if (resizeAndOptimize.IsVisible)
                                {
                                    return; // Prevent paste errors
                                }
                            }

                            if (shiftDown)
                            {
                                Base64.SendToClipboard();
                            }
                            else if (altDown)
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
                            ImageCropping.StartCrop();
                        }
                        break;

                    // Ctrl + V
                    case Key.V:
                        if (ctrlDown)
                        {
                            Paste();
                        }

                        break;

                    // Ctrl + I
                    case Key.I:
                        if (ctrlDown)
                        {
                            NativeMethods.ShowFileProperties(Pics[FolderIndex]);
                        }

                        break;

                    // Ctrl + P
                    case Key.P:
                        if (ctrlDown)
                        {
                            Print(Pics[FolderIndex]);
                        }

                        break;

                    // Ctrl + R
                    case Key.R:
                        if (ctrlDown)
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
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsOpen) { break; }
                        Tooltip.ShowTooltipMessage("Set to center image in window");
                        Configs.SetScalingBehaviour(false, false);
                        break;

                    // 2
                    case Key.D2:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsOpen) { break; }
                        Tooltip.ShowTooltipMessage("Center image in window, fill height");
                        Configs.SetScalingBehaviour(false, true);
                        break;

                    // 3
                    case Key.D3:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsOpen) { break; }
                        Tooltip.ShowTooltipMessage("Center application to window");
                        Configs.SetScalingBehaviour(true, false);
                        break;

                    // 4
                    case Key.D4:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsOpen) { break; }
                        Tooltip.ShowTooltipMessage("Center application to window, fill height");
                        Configs.SetScalingBehaviour(true, true);
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
            if (altDown && (e.SystemKey == Key.Z))
            {
                if (!e.IsRepeat)
                {
                    HideInterfaceLogic.ToggleInterface();
                }
                //e.Handled = true;
            }

            // Alt + Enter
            else if (altDown && (e.SystemKey == Key.Enter))
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


    }
}