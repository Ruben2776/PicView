using PicView.Editing;
using PicView.Editing.Crop;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.UILogic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.Error_Handling;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.Copy_Paste;
using static PicView.FileHandling.DeleteFiles;
using static PicView.FileHandling.Open_Save;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.TransformImage.Rotation;
using static PicView.UILogic.TransformImage.Scroll;
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UC;

namespace PicView.Shortcuts
{
    internal static class MainShortcuts
    {
        internal static async Task MainWindow_KeysDownAsync(object sender, KeyEventArgs e)
        {
            // Don't allow keys when typing in text
            if (GetMainWindow.TitleText.IsKeyboardFocusWithin) { return; }

            var ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            var altDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            var shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            // Don't execute keys when entering in GoToPicBox
            if (GetImageSettingsMenu.GoToPic != null)
            {
                if (GetImageSettingsMenu.GoToPic.GoToPicBox.IsKeyboardFocusWithin)
                {
                    return;
                }
            }

            #region CroppingKeys

            if (GetCropppingTool != null)
            {
                if (GetCropppingTool.IsVisible)
                {
                    if (e.Key == Key.Escape)
                    {
                        CropFunctions.CloseCrop();
                        e.Handled = true;
                        return;
                    }

                    if (e.Key == Key.Enter)
                    {
                        await CropFunctions.PerformCropAsync().ConfigureAwait(false);
                        e.Handled = true;
                        return;
                    }
                    e.Handled = true;
                    return;
                }
            }

            #endregion CroppingKeys

            #region Keys where it can be held down

            switch (e.Key)
            {
                case Key.BrowserForward:
                case Key.Right:
                case Key.D:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen && !Properties.Settings.Default.FullscreenGallery)
                        {
                            return;
                        }
                    }
                    // Go to first if Ctrl held down
                    if (ctrlDown && !e.IsRepeat)
                    {
                        await PicAsync(true, true).ConfigureAwait(false);
                    }
                    else
                    {
                        FastPicRunning = e.IsRepeat; // Report if key held down
                        await PicAsync().ConfigureAwait(false);
                    }
                    return;

                case Key.BrowserBack:
                case Key.Left:
                case Key.A:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen && !Properties.Settings.Default.FullscreenGallery)
                        {
                            return;
                        }
                    }
                    // Go to last if Ctrl held down
                    if (ctrlDown && !e.IsRepeat)
                    {
                        await PicAsync(false, true).ConfigureAwait(false);
                    }
                    else
                    {
                        FastPicRunning = e.IsRepeat; // Report if key held down
                        await PicAsync(false).ConfigureAwait(false);
                    }
                    return;

                case Key.PageUp:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            GalleryNavigation.ScrollTo(true, ctrlDown);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                    }

                    return;

                case Key.PageDown:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            GalleryNavigation.ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + 30);
                    }

                    return;

                case Key.Up:
                case Key.W:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            GalleryNavigation.ScrollTo(true, ctrlDown);
                        }
                        else
                        {
                            Rotate(true);
                        }
                    }
                    else
                    {
                        Rotate(false);
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                    }
                    return;

                case Key.Down:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            GalleryNavigation.ScrollTo(false, ctrlDown);
                        }
                        else
                        {
                            Rotate(true);
                        }
                    }
                    else if (Properties.Settings.Default.ScrollEnabled)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + 30);
                    }
                    else
                    {
                        Rotate(true);
                    }
                    return;

                case Key.S:
                    if (ctrlDown && !GalleryFunctions.IsOpen)
                    {
                        await SaveFilesAsync().ConfigureAwait(false);
                    }
                    else if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsOpen)
                        {
                            GalleryNavigation.ScrollTo(false, ctrlDown);
                        }
                        else
                        {
                            Rotate(false);
                        }
                    }
                    else if (Properties.Settings.Default.ScrollEnabled)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + 30);
                    }
                    else
                    {
                        Rotate(false);
                    }
                    return;

                // Zoom
                case Key.Add:
                case Key.OemPlus:
                    await ZoomAsync(true).ConfigureAwait(false);
                    return;

                case Key.Subtract:
                case Key.OemMinus:
                    await ZoomAsync(false).ConfigureAwait(false);
                    return;

                default: break;
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
                        }
                        else if (Properties.Settings.Default.Fullscreen)
                        {
                            if (GalleryFunctions.IsOpen)
                            {
                                await GalleryToggle.ToggleAsync().ConfigureAwait(false);
                            }
                            else
                            {
                                Fullscreen_Restore();
                            }
                        }
                        else if (Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                        {
                            Slideshow.StopSlideshow();
                        }
                        else if (GalleryFunctions.IsOpen)
                        {
                            await GalleryToggle.ToggleAsync().ConfigureAwait(false);
                        }
                        else if (IsDialogOpen)
                        {
                            IsDialogOpen = false;
                        }
                        else if (Color_Picking.IsRunning)
                        {
                            await Color_Picking.StopRunningAsync(false).ConfigureAwait(false);
                        }
                        else if (GetEffectsWindow != null && GetEffectsWindow.IsVisible)
                        {
                            GetEffectsWindow.Hide();
                        }
                        else if (GetImageInfoWindow != null && GetImageInfoWindow.IsVisible)
                        {
                            GetImageInfoWindow.Hide();
                        }
                        else if (GetInfoWindow != null && GetInfoWindow.IsVisible)
                        {
                            GetInfoWindow.Hide();
                        }
                        else if (GetSettingsWindow != null && GetSettingsWindow.IsVisible)
                        {
                            GetSettingsWindow.Hide();
                        }
                        else if (!MainContextMenu.IsVisible)
                        {
                            if (GetCropppingTool != null && GetCropppingTool.IsVisible)
                            {
                                return;
                            }
                            SystemCommands.CloseWindow(GetMainWindow);
                        }
                        break;

                    // Ctrl + Q
                    case Key.Q:
                        if (ctrlDown)
                        {
                            SystemCommands.CloseWindow(GetMainWindow);
                        }
                        break;

                    // O, Ctrl + O
                    case Key.O:
                        await OpenAsync().ConfigureAwait(false);
                        break;

                    // X, Ctrl + X
                    case Key.X:
                        if (ctrlDown && !GalleryFunctions.IsOpen)
                        {
                            Cut(Pics[FolderIndex]);
                        }
                        else
                        {
                            ConfigureSettings.UpdateUIValues.SetScrolling(sender, e);
                        }
                        break;
                    // F
                    case Key.F:
                        if (!GalleryFunctions.IsOpen)
                        {
                            Flip();
                        }
                        break;

                    // Delete, Shift + Delete
                    case Key.Delete:
                        if (!GalleryFunctions.IsOpen)
                        {
                            await DeleteFileAsync(!shiftDown).ConfigureAwait(false);
                        }
                        break;

                    // Ctrl + C, Ctrl + Shift + C, Ctrl + Alt + C
                    case Key.C:
                        if (ctrlDown && !GalleryFunctions.IsOpen)
                        {
                            if (shiftDown)
                            {
                                Copyfile();
                            }
                            else if (altDown)
                            {
                                System.Threading.Tasks.Task task = Base64.SendToClipboard();
                            }
                            else
                            {
                                CopyBitmap();
                            }
                        }
                        else if (!GalleryFunctions.IsOpen)
                        {
                            CropFunctions.StartCrop();
                        }
                        break;

                    // Ctrl + V
                    case Key.V:
                        if (ctrlDown && !GalleryFunctions.IsOpen)
                        {
                            await PasteAsync().ConfigureAwait(false);
                        }
                        break;

                    // Ctrl + I
                    case Key.I:
                        if (ctrlDown && !GalleryFunctions.IsOpen)
                        {
                            SystemIntegration.NativeMethods.ShowFileProperties(Pics[FolderIndex]);
                        }
                        break;

                    // Ctrl + P
                    case Key.P:
                        if (ctrlDown && !GalleryFunctions.IsOpen)
                        {
                            Print(Pics[FolderIndex]);
                        }
                        break;

                    //  R
                    case Key.R:
                        if (ctrlDown && !GalleryFunctions.IsOpen)
                        {
                            await ReloadAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            ResetZoom();
                        }
                        break;

                    // L
                    case Key.L:
                        ConfigureSettings.UpdateUIValues.SetLooping();
                        break;

                    // E
                    case Key.E:
                        if (!GalleryFunctions.IsOpen)
                        {
                            OpenWith(Pics[FolderIndex]);
                        }
                        break;

                    // T
                    case Key.T:
                        if (!GalleryFunctions.IsOpen)
                        {
                            ConfigureSettings.ConfigColors.ChangeBackground(sender, e);
                        }
                        break;

                    // N
                    case Key.N:
                        if (ctrlDown)
                        {
                            ProcessHandling.ProcessLogic.StartNewProcess();
                        }
                        break;

                    // G
                    case Key.G:
                        if (Properties.Settings.Default.FullscreenGallery == false
                          && !GetQuickSettingsMenu.IsVisible
                          && !GetToolsAndEffectsMenu.IsVisible
                          && !GetFileMenu.IsVisible
                          && !GetImageSettingsMenu.IsVisible)
                        {
                            await GalleryToggle.ToggleAsync().ConfigureAwait(false);
                        }
                        break;

                    // Space
                    case Key.Space:
                        if (GetPicGallery != null)
                        {
                            if (GalleryFunctions.IsOpen)
                            {
                                GalleryNavigation.ScrollTo();
                                return;
                            }
                        }
                        CenterWindowOnScreen();
                        break;

                    // 1
                    case Key.D1:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsOpen
                        || Properties.Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["AutoFitWindowMessage"]);
                        await ConfigureSettings.UpdateUIValues.SetScalingBehaviourAsync(true, false).ConfigureAwait(false);
                        break;

                    // 2
                    case Key.D2:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsOpen
                        || Properties.Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["AutoFitWindowFillHeight"]);
                        await ConfigureSettings.UpdateUIValues.SetScalingBehaviourAsync(true, true).ConfigureAwait(false);
                        break;

                    // 3
                    case Key.D3:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsOpen
                        || Properties.Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["NormalWindowBehavior"]);
                        await ConfigureSettings.UpdateUIValues.SetScalingBehaviourAsync(false, false).ConfigureAwait(false);
                        break;

                    // 4
                    case Key.D4:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsOpen
                        || Properties.Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["NormalWindowBehaviorFillHeight"]);
                        await ConfigureSettings.UpdateUIValues.SetScalingBehaviourAsync(false, true).ConfigureAwait(false);
                        break;

                    // F1
                    case Key.F1:
                        InfoWindow();
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
                        if (!GalleryFunctions.IsOpen)
                        {
                            Slideshow.StartSlideshow();
                        }
                        break;

                    // F6
                    case Key.F6:
                        EffectsWindow();
                        break;

                    // F7
                    case Key.F7:
                        ResetZoom();
                        break;

#if DEBUG
                    // F8
                    case Key.F8:
                        Unload();
                        break;
#endif
                    // F11
                    case Key.F11:
                        Fullscreen_Restore();
                        break;

                    // Home
                    case Key.Home:
                        GetMainWindow.Scroller.ScrollToHome();
                        break;

                    // End
                    case Key.End:
                        GetMainWindow.Scroller.ScrollToEnd();
                        break;

                    default: break;
                }
            }

            #endregion Key is not held down

            #region Alt + keys

            // Alt doesn't work in switch? Waiting for key up is confusing in this case

            if (altDown && !e.IsRepeat)
            {
                // Alt + Z
                if ((e.SystemKey == Key.Z) && !GalleryFunctions.IsOpen)
                {
                    HideInterfaceLogic.ToggleInterface();
                }

                // Alt + Enter
                else if ((e.SystemKey == Key.Enter))
                {
                    if (Properties.Settings.Default.FullscreenGallery == false)
                    {
                        Fullscreen_Restore();
                    }
                }
            }

            #endregion Alt + keys
        }

        internal static async Task MainWindow_KeysUpAsync(object sender, KeyEventArgs e)
        {
            // Don't allow keys when typing in text
            if (GetMainWindow.TitleText.IsKeyboardFocusWithin) { return; }

            switch (e.Key)
            {
                case Key.A:
                case Key.Right:
                case Key.Left:
                case Key.D:
                    if (FolderIndex <= 0 || Pics?.Count < FolderIndex)
                    {
                        return;
                    }
                    await FastPicUpdateAsync().ConfigureAwait(false);
                    break;

                default: break;
            }
        }

        internal static async Task MainWindow_MouseDownAsync(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                    // Stop running color picking when right clicking
                    if (Color_Picking.IsRunning)
                    {
                        await Color_Picking.StopRunningAsync(false).ConfigureAwait(false);
                    }
                    else if (IsAutoScrolling)
                    {
                        StopAutoScroll();
                        return;
                    }
                    break;

                case MouseButton.Left:
                    if (IsAutoScrolling)
                    {
                        StopAutoScroll();
                        return;
                    }
                    break;

                case MouseButton.Middle:
                    if (!IsAutoScrolling)
                    {
                        StartAutoScroll(e);
                    }
                    else
                    {
                        StopAutoScroll();
                    }

                    break;

                case MouseButton.XButton1:
                    await PicAsync(false).ConfigureAwait(false);
                    break;

                case MouseButton.XButton2:
                    await PicAsync().ConfigureAwait(false);
                    break;

                default: break;
            }
        }

        /// <summary>
        /// Pan and Zoom, reset zoom and double click to reset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Move window when Shift is being held down
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift || !Properties.Settings.Default.ShowInterface)
            {
                Move(sender, e);
                return;
            }

            // Fix focus
            EditTitleBar.Refocus();

            // Logic for auto scrolling
            if (IsAutoScrolling)
            {
                // Report position and enable autoscrolltimer
                AutoScrollOrigin = e.GetPosition(GetMainWindow);
                AutoScrollTimer.Enabled = true;
                return;
            }
            // Reset zoom on double click
            if (e.ClickCount == 2)
            {
                ResetZoom();
                return;
            }
            // Drag logic
            if (Properties.Settings.Default.ScrollEnabled == false)
            {
                PreparePanImage(sender, e);
            }
        }

        internal static async Task Bg_MouseLeftButtonDownAsync(object sender, MouseButtonEventArgs e)
        {
            if (GetMainWindow.TitleText.InnerTextBox.IsKeyboardFocusWithin)
            {
                // Fix focus
                EditTitleBar.Refocus();
                return;
            }

            // Move window when Shift is being held down
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                Move(sender, e);
                return;
            }

            if (Color_Picking.IsRunning)
            {
                await Color_Picking.StopRunningAsync(true).ConfigureAwait(false);
            }

            // Reset zoom on double click
            if (e.ClickCount == 2)
            {
                ResetZoom();
                return;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Stop autoscrolling or dragging image
            if (IsAutoScrolling)
            {
                StopAutoScroll();
            }
            else
            {
                GetMainWindow.MainImage.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// Used to drag image
        /// or getting position for autoscrolltimer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsAutoScrolling)
            {
                // Start automainWindow.Scroller and report position
                AutoScrollPos = e.GetPosition(GetMainWindow.Scroller);
                AutoScrollTimer.Start();
            }

            if (Color_Picking.IsRunning)
            {
                if (GetColorPicker.Opacity == 1)
                {
                    Color_Picking.StartRunning();
                }

                return;
            }

            PanImage(sender, e);
        }

        /// <summary>
        /// Zooms, scrolls or changes image with mousewheel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static async Task MainImage_MouseWheelAsync(object sender, MouseWheelEventArgs e)
        {
            // Disable normal scroll, so we can use our own values
            e.Handled = true;

            // Make sure not to fire off events when autoscrolling
            if (IsAutoScrolling)
            {
                return;
            }

            if (GalleryFunctions.IsOpen)
            {
                GalleryNavigation.ScrollTo(sender, e);
            }
            else if (Properties.Settings.Default.ScrollEnabled)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    await PicAsync(e.Delta > 0).ConfigureAwait(false);
                }
                else
                {
                    if (GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed)
                    {
                        if (Properties.Settings.Default.CtrlZoom == false)
                        {
                            return;
                        }
                        await PicAsync(e.Delta > 0).ConfigureAwait(false);
                    }
                    if (GetMainWindow.CheckAccess() == false)
                    {
                        return;
                    }
                    // Scroll vertical when scroll enabled
                    var zoomSpeed = 45;
                    if (e.Delta > 0)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - zoomSpeed);
                    }
                    else if (e.Delta < 0)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + zoomSpeed);
                    }
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (Properties.Settings.Default.CtrlZoom)
                {
                    await ZoomAsync(e.Delta > 0).ConfigureAwait(false);
                }
                else
                {
                    await PicAsync(e.Delta > 0).ConfigureAwait(false);
                }
            }
            else
            {
                if (Properties.Settings.Default.CtrlZoom)
                {
                    await PicAsync(e.Delta > 0).ConfigureAwait(false);
                }
                else
                {
                    await ZoomAsync(e.Delta > 0).ConfigureAwait(false);
                }
            }
        }
    }
}