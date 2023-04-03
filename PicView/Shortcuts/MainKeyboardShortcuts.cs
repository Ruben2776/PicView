using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.ConfigureSettings;
using PicView.Editing;
using PicView.Editing.Crop;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.ProcessHandling;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.Views.UserControls.Buttons;
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.ErrorHandling;
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

            if (GetCropppingTool is { IsVisible: true })
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        CropFunctions.CloseCrop();
                        e.Handled = true;
                        return;

                    case Key.Enter:
                        await CropFunctions.PerformCropAsync().ConfigureAwait(false);
                        e.Handled = true;
                        return;

                    default:
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
                        await Navigation.GoToNextImage(NavigateTo.Last).ConfigureAwait(false);
                    }
                    else
                    {
                        FastPicRunning = e.IsRepeat; // Report if key held down
                        await Navigation.GoToNextImage(NavigateTo.Next, FastPicRunning).ConfigureAwait(false);
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
                        await Navigation.GoToNextImage(NavigateTo.First).ConfigureAwait(false);
                    }
                    else
                    {
                        FastPicRunning = e.IsRepeat; // Report if key held down
                        await Navigation.GoToNextImage(NavigateTo.Previous, FastPicRunning).ConfigureAwait(false);
                    }
                    return;

                case Key.PageUp:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
                        {
                            GalleryNavigation.ScrollTo(true, ctrlDown);
                            return;
                        }
                    }
                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                    }

                    return;

                case Key.PageDown:
                    if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
                        {
                            GalleryNavigation.ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    return;

                case Key.Up:
                case Key.W:
                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                    }
                    else if (GalleryFunctions.IsHorizontalOpen && GetPicGallery != null)
                    {
                        GalleryNavigation.HorizontalNavigation(GalleryNavigation.Direction.Up);
                    }
                    else
                    {
                        Rotate(e.IsRepeat, false);
                    }
                    return;

                case Key.Down:
                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
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
                            Rotate(e.IsRepeat, true);
                        }
                    }
                    else
                    {
                        Rotate(e.IsRepeat, true);
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
                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + 30);
                    }
                    else
                    {
                        Rotate(e.IsRepeat, false);
                    }
                    return;

                // Zoom
                case Key.Add:
                case Key.OemPlus:
                    if (!GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
                    {
                        Zoom(true);
                    }
                    return;

                case Key.Subtract:
                case Key.OemMinus:
                    if (!GalleryFunctions.IsHorizontalFullscreenOpen || !GalleryFunctions.IsHorizontalOpen)
                    {
                        Zoom(false);
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
                        }
                        else if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
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
                            Color_Picking.StopRunning(false);
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
                        else if (Settings.Default.Fullscreen)
                        {
                            WindowSizing.Fullscreen_Restore(false);
                        }
                        else if (GetQuickResize is not null && GetQuickResize.Opacity > 0)
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
                            ConfigColors.ChangeBackground();
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
                                UpdateUIValues.SetScrolling();
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

                    // J
                    case Key.J:
                        if (!GalleryFunctions.IsHorizontalOpen)
                        {
                            ResizeButton.ToggleQuickResize();
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
                                CopyBitmap(); 
                            }
                            else if (altDown)
                            {
                                CopyFilePath();
                            }
                            else
                            {
                                if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
                                    CopyBitmap();
                                else
                                    CopyFile();
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
                            await PasteAsync().ConfigureAwait(false);
                        }
                        break;

                    case Key.I:
                        if (ctrlDown && !GalleryFunctions.IsHorizontalOpen)
                        {
                            if (altDown)
                            {
                                ResizeButton.ToggleQuickResize();
                            }
                            else
                            {
                                FileProperties.ShowFileProperties();
                            }
                        }
                        else
                        {
                            ImageInfoWindow();
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
                        UpdateUIValues.SetLooping();
                        break;

                    // E
                    case Key.E:
                        if (!GalleryFunctions.IsHorizontalOpen)
                        {
                            if (CheckOutOfRange() == false)
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
                        UpdateUIValues.SetTopMost();
                        break;

                    // N
                    case Key.N:
                        if (ctrlDown)
                        {
                            ProcessLogic.StartNewProcess();
                        }
                        else
                        {
                            ResizeWindow();
                        }
                        break;

                    // G
                    case Key.G:
                        if (GalleryFunctions.IsHorizontalOpen)
                        {
                            GalleryToggle.CloseHorizontalGallery();
                        }
                        else if (GalleryFunctions.IsHorizontalFullscreenOpen == false)
                        {
                            await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
                        }
                        break;

                    // Space
                    case Key.Space:
                        if (GetPicGallery != null)
                        {
                            if (GalleryFunctions.IsHorizontalOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
                            {
                                GalleryNavigation.ScrollTo();
                                return;
                            }
                        }
                        WindowSizing.CenterWindowOnScreen();
                        break;

                    // 1
                    case Key.D1:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsHorizontalOpen
                        || Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["AutoFitWindowMessage"]);
                        UpdateUIValues.SetScalingBehaviour(true, false);
                        break;

                    // 2
                    case Key.D2:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsHorizontalOpen
                        || Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["AutoFitWindowFillHeight"]);
                        UpdateUIValues.SetScalingBehaviour(true, true);
                        break;

                    // 3
                    case Key.D3:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsHorizontalOpen
                        || Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["NormalWindowBehavior"]);
                        UpdateUIValues.SetScalingBehaviour(false, false);
                        break;

                    // 4
                    case Key.D4:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsHorizontalOpen
                        || Settings.Default.Fullscreen) { break; }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["NormalWindowBehaviorFillHeight"]);
                        UpdateUIValues.SetScalingBehaviour(false, true);
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
                        if (!GalleryFunctions.IsHorizontalOpen || !GalleryFunctions.IsHorizontalFullscreenOpen)
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

                    // F9
                    case Key.F9:
                        ResizeWindow();
                        break;

                    // F11
                    case Key.F11:
                        WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen);
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
                        if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsHorizontalOpen)
                        {
                            await GalleryClick.ClickAsync(GalleryNavigation.SelectedGalleryItem).ConfigureAwait(false);
                        }
                        break;
                }
            }

            #endregion Key is not held down
        }

        internal static void MainWindow_KeysUp(object sender, KeyEventArgs e)
        {
            // Don't allow keys when typing in text
            if (GetMainWindow.TitleText.IsKeyboardFocusWithin) { return; }

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
            {
                if (e.SystemKey == Key.Z)
                {
                    if (GalleryFunctions.IsHorizontalOpen || !GalleryFunctions.IsHorizontalFullscreenOpen)
                    {
                        HideInterfaceLogic.ToggleInterface();
                    }
                }
                else if (e.SystemKey == Key.Enter)
                {
                    if (Settings.Default.FullscreenGalleryHorizontal == false)
                    {
                        WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen);
                    }
                }
                return;
            }

            switch (e.Key)
            {
                case Key.A:
                case Key.Right:
                case Key.Left:
                case Key.D:
                    if (FolderIndex < 0 || FolderIndex >= Pics.Count) return;
                    _ = FastPic.FastPicUpdateAsync().ConfigureAwait(false);
                    return;
            }
        }
    }
}