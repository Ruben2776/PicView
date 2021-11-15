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
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UC;

namespace PicView.Shortcuts
{
    internal static class MainKeyboardShortcuts
    {
        internal static async Task MainWindow_KeysDownAsync(object sender, KeyEventArgs e)
        {
            // Don't allow keys when typing in text
            if (GetMainWindow.TitleText.IsKeyboardFocusWithin) { return; }

            // Don't execute keys when entering in GoToPicBox || QuickResize
            if (GetImageSettingsMenu.GoToPic != null)
            {
                if (GetImageSettingsMenu.GoToPic.GoToPicBox.IsKeyboardFocusWithin)
                {
                    return;
                }
            }

            if (GetQuickResize != null)
            {
                if (GetQuickResize.WidthBox.IsKeyboardFocused || GetQuickResize.HeightBox.IsKeyboardFocused)
                {
                    return;
                }
            }

            var ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            var altDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            var shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

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
                    // exit if browsing horizontal PicGallery
                    if (GalleryFunctions.IsHorizontalOpen)
                    {
                        GalleryNavigation.HorizontalNavigation(GalleryNavigation.Direction.Right);
                        return;
                    }
                    // Go to first if Ctrl held down
                    if (ctrlDown && !e.IsRepeat)
                    {
                        await NavigateToPicAsync(true, true).ConfigureAwait(false);
                    }
                    else
                    {
                        FastPicRunning = e.IsRepeat; // Report if key held down
                        await NavigateToPicAsync(true, false, e.IsRepeat).ConfigureAwait(false);
                    }
                    return;

                case Key.BrowserBack:
                case Key.Left:
                case Key.A:
                    if (GalleryFunctions.IsHorizontalOpen)
                    {
                        GalleryNavigation.HorizontalNavigation(GalleryNavigation.Direction.Left);
                        return;
                    }
                    // Go to last if Ctrl held down
                    if (ctrlDown && !e.IsRepeat)
                    {
                        await NavigateToPicAsync(false, true).ConfigureAwait(false);
                    }
                    else
                    {
                        await NavigateToPicAsync(false, false, e.IsRepeat).ConfigureAwait(false);
                    }
                    return;

                case Key.PageUp:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
                        {
                            GalleryNavigation.ScrollTo(true, ctrlDown);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled && ConfigureWindows.GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                    }

                    return;

                case Key.PageDown:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
                        {
                            GalleryNavigation.ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    return;

                case Key.Up:
                case Key.W:
                    if (Properties.Settings.Default.ScrollEnabled && ConfigureWindows.GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                    }
                    else if (GalleryFunctions.IsHorizontalOpen && GetPicGallery != null)
                    {
                        GalleryNavigation.HorizontalNavigation(GalleryNavigation.Direction.Up);
                    }
                    else
                    {
                        Rotate(false);
                    }
                    return;

                case Key.Down:
                    if (Properties.Settings.Default.ScrollEnabled && ConfigureWindows.GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + 30);
                    }
                    else if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsHorizontalOpen)
                        {
                            GalleryNavigation.HorizontalNavigation(GalleryNavigation.Direction.Down);
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

                case Key.S:
                    if (ctrlDown && !GalleryFunctions.IsHorizontalOpen)
                    {
                        await SaveFilesAsync().ConfigureAwait(false);
                        return; // Fix saving file
                    }
                    else if (GalleryFunctions.IsHorizontalOpen)
                    {
                        GalleryNavigation.HorizontalNavigation(GalleryNavigation.Direction.Down);
                        return;
                    }
                    if (Properties.Settings.Default.ScrollEnabled && ConfigureWindows.GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
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
                    if (!GalleryFunctions.IsHorizontalFullscreenOpen || !GalleryFunctions.IsVerticalFullscreenOpen || !GalleryFunctions.IsHorizontalOpen)
                    {
                        Zoom(true);
                    }
                    return;

                case Key.Subtract:
                case Key.OemMinus:
                    if (!GalleryFunctions.IsHorizontalFullscreenOpen || !GalleryFunctions.IsVerticalFullscreenOpen || !GalleryFunctions.IsHorizontalOpen)
                    {
                        Zoom(false);
                    }
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
                        else if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
                        {
                            GalleryToggle.CloseCurrentGallery();
                        }
                        else if (Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                        {
                            Slideshow.StopSlideshow();
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
                        else if (Properties.Settings.Default.Fullscreen)
                        {
                            UILogic.Sizing.WindowSizing.Fullscreen_Restore();
                        }
                        if (GetQuickResize is not null && GetQuickResize.Opacity > 0)
                        {
                            GetQuickResize.Hide();
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

                    // B
                    case Key.B:
                        if (!GalleryFunctions.IsHorizontalOpen)
                        {
                            ConfigureSettings.ConfigColors.ChangeBackground();
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
                        if (GalleryFunctions.IsHorizontalOpen is false)
                        {
                            if (ctrlDown)
                            {
                                Cut();
                            }
                            else
                            {
                                ConfigureSettings.UpdateUIValues.SetScrolling();
                            }
                        }
                        break;
                    // F
                    case Key.F:
                        if (!GalleryFunctions.IsHorizontalOpen)
                        {
                            Flip();
                        }
                        break;

                    // Delete, Shift + Delete
                    case Key.Delete:
                        if (!GalleryFunctions.IsHorizontalOpen)
                        {
                            await DeleteFileAsync(!shiftDown).ConfigureAwait(false);
                        }
                        break;

                    // Ctrl + C, Ctrl + Shift + C, Ctrl + Alt + C
                    case Key.C:
                        if (ctrlDown && !GalleryFunctions.IsHorizontalOpen)
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
                        else if (!GalleryFunctions.IsHorizontalOpen)
                        {
                            CropFunctions.StartCrop();
                        }
                        break;

                    // Ctrl + V
                    case Key.V:
                        if (ctrlDown && !GalleryFunctions.IsHorizontalOpen)
                        {
                            Paste();
                        }
                        break;

                    case Key.I:
                        if (ctrlDown && !GalleryFunctions.IsHorizontalOpen)
                        {
                            SystemIntegration.NativeMethods.ShowFileProperties(Pics[FolderIndex]);
                        }
                        else
                        {
                            Views.UserControls.ResizeButton.ToggleQuickResize();
                        }
                        break;

                    // Ctrl + P
                    case Key.P:
                        if (ctrlDown && !GalleryFunctions.IsHorizontalOpen)
                        {
                            Print(Pics[FolderIndex]);
                        }
                        break;

                    //  R
                    case Key.R:
                        if (ctrlDown && !GalleryFunctions.IsHorizontalOpen)
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
                        if (!GalleryFunctions.IsHorizontalOpen)
                        {
                            if (ChangeImage.Error_Handling.CheckOutOfRange() == false)
                            {
                                OpenWith(Pics[FolderIndex]);
                            }
                        }
                        else
                        {
                            await GalleryClick.ClickAsync(GalleryNavigation.SelectedGalleryItem).ConfigureAwait(false);
                        }
                        break;

                    // T
                    case Key.T:
                        ConfigureSettings.UpdateUIValues.SetTopMost();
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
                        if (GalleryFunctions.IsHorizontalOpen)
                        {
                            GalleryToggle.CloseHorizontalGallery();
                        }
                        else if (GalleryFunctions.IsVerticalFullscreenOpen == false && GalleryFunctions.IsHorizontalFullscreenOpen == false)
                        {
                            await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
                        }
                        break;

                    // Space
                    case Key.Space:
                        if (GetPicGallery != null)
                        {
                            if (GalleryFunctions.IsHorizontalOpen || GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
                            {
                                GalleryNavigation.ScrollTo();
                                return;
                            }
                        }
                        UILogic.Sizing.WindowSizing.CenterWindowOnScreen();
                        break;

                    // 1
                    case Key.D1:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsHorizontalOpen
                        || Properties.Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["AutoFitWindowMessage"]);
                        await ConfigureSettings.UpdateUIValues.SetScalingBehaviourAsync(true, false).ConfigureAwait(false);
                        break;

                    // 2
                    case Key.D2:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsHorizontalOpen
                        || Properties.Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["AutoFitWindowFillHeight"]);
                        await ConfigureSettings.UpdateUIValues.SetScalingBehaviourAsync(true, true).ConfigureAwait(false);
                        break;

                    // 3
                    case Key.D3:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsHorizontalOpen
                        || Properties.Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["NormalWindowBehavior"]);
                        await ConfigureSettings.UpdateUIValues.SetScalingBehaviourAsync(false, false).ConfigureAwait(false);
                        break;

                    // 4
                    case Key.D4:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsHorizontalOpen
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
                        if (!GalleryFunctions.IsHorizontalOpen || !GalleryFunctions.IsVerticalFullscreenOpen || !GalleryFunctions.IsHorizontalFullscreenOpen)
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

                    // F11
                    case Key.F11:
                        UILogic.Sizing.WindowSizing.Fullscreen_Restore();
                        break;

                    // Home
                    case Key.Home:
                        GetMainWindow.Scroller.ScrollToHome();
                        break;

                    // End
                    case Key.End:
                        GetMainWindow.Scroller.ScrollToEnd();
                        break;

                    // Enter
                    case Key.Enter:
                        if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
                        {
                            await GalleryClick.ClickAsync(GalleryNavigation.SelectedGalleryItem).ConfigureAwait(false);
                        }
                        break;


                    default: break;
                }
            }

            #endregion Key is not held down
        }

        internal static void MainWindow_KeysUp(object sender, KeyEventArgs e)
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
                    _ = ChangeImage.FastPic.FastPicUpdateAsync().ConfigureAwait(false);
                    return;

                default: break;
            }

            var altDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            if (altDown && !e.IsRepeat)
            {
                // Alt + Z
                if ((e.SystemKey == Key.Z) && !GalleryFunctions.IsHorizontalOpen || !GalleryFunctions.IsVerticalFullscreenOpen || !GalleryFunctions.IsHorizontalFullscreenOpen)
                {
                    HideInterfaceLogic.ToggleInterface();
                }

                // Alt + Enter
                else if ((e.SystemKey == Key.Enter))
                {
                    if (Properties.Settings.Default.FullscreenGalleryHorizontal == false)
                    {
                        UILogic.Sizing.WindowSizing.Fullscreen_Restore();
                    }
                }
            }
        }
    }
}