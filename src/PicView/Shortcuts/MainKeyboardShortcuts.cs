using System.IO;
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
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.CopyPaste;
using static PicView.FileHandling.DeleteFiles;
using static PicView.FileHandling.OpenSave;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.TransformImage.Rotation;
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UC;

namespace PicView.Shortcuts
{
    internal static class MainKeyboardShortcuts
    {
        internal static bool IsKeyHeldDown { get; private set; }
        internal static bool CtrlDown { get; private set; }
        internal static bool AltDown { get; private set; }
        internal static bool ShiftDown { get; private set; }

        internal static async Task MainWindow_KeysDownAsync(object sender, KeyEventArgs e)
        {
            #region return statements

            // Don't allow keys when typing in text
            if (GetMainWindow.TitleText.IsKeyboardFocusWithin)
            {
                return;
            }

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

            #endregion return statements

            CtrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            AltDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            ShiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            #region CroppingKeys

            if (GetCroppingTool is { IsVisible: true })
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

                    case Key.C:
                        if (CtrlDown)
                        {
                            CropFunctions.CopyCrop();
                        }

                        return;

                    default:
                        e.Handled = true;
                        return;
                }
            }

            #endregion CroppingKeys

            // Capture the pressed key and modifiers
            var capturedKey = e.Key;
            var modifiers = Keyboard.Modifiers;
            IsKeyHeldDown = e.IsRepeat;

            // Check if the captured key and modifiers match any user-defined shortcuts
            foreach (var shortcut in CustomKeybindings.CustomShortcuts.Where(shortcut => shortcut.Key == capturedKey && Keyboard.Modifiers == modifiers))
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (shortcut.Value is null)
                {
                    return;
                }
                // Execute the associated action asynchronously
                await shortcut.Value.Invoke().ConfigureAwait(false);
                return;
            }

            return;

            #region Keys where it can be held down

            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility ==
                        Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - 30);
                    }
                    else if (GalleryFunctions.IsGalleryOpen && GetPicGallery != null)
                    {
                        GalleryNavigation.NavigateGallery(GalleryNavigation.Direction.Up);
                    }
                    else
                    {
                        Rotate(e.IsRepeat, false);
                    }

                    return;

                case Key.Down:
                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility ==
                        Visibility.Visible)
                    {
                        GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + 30);
                    }
                    else if (GetPicGallery != null)
                    {
                        if (GalleryFunctions.IsGalleryOpen)
                        {
                            GalleryNavigation.NavigateGallery(GalleryNavigation.Direction.Down);
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
                    if (CtrlDown && !GalleryFunctions.IsGalleryOpen)
                    {
                        if (ShiftDown)
                        {
                            await SaveFilesAsync(false).ConfigureAwait(false);
                        }
                        else
                        {
                            await SaveFilesAsync(Settings.Default.ShowFileSavingDialog).ConfigureAwait(false);
                        }

                        return; // Fix saving file
                    }

                    if (GalleryFunctions.IsGalleryOpen)
                    {
                        GalleryNavigation.NavigateGallery(GalleryNavigation.Direction.Down);
                        return;
                    }

                    if (Settings.Default.ScrollEnabled && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility ==
                        Visibility.Visible)
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
                    Zoom(true);
                    return;

                case Key.Subtract:
                case Key.OemMinus:
                    Zoom(false);
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
                        else if (GalleryFunctions.IsGalleryOpen)
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
                        else if (ColorPicking.IsRunning)
                        {
                            ColorPicking.StopRunning(false);
                        }
                        else if (GetEffectsWindow is { IsVisible: true })
                        {
                            GetEffectsWindow.Hide();
                        }
                        else if (GetImageInfoWindow is { IsVisible: true })
                        {
                            GetImageInfoWindow.Hide();
                        }
                        else if (GetAboutWindow is { IsVisible: true })
                        {
                            GetAboutWindow.Hide();
                        }
                        else if (GetSettingsWindow is { IsVisible: true })
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
                            if (GetCroppingTool is { IsVisible: true })
                            {
                                return;
                            }

                            SystemCommands.CloseWindow(GetMainWindow);
                        }

                        break;

                    // Ctrl + Q
                    case Key.Q:
                        if (CtrlDown)
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
                        if (GalleryFunctions.IsGalleryOpen is false)
                        {
                            if (CtrlDown)
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
                        if (!GalleryFunctions.IsGalleryOpen)
                        {
                            Flip();
                        }

                        break;

                    // J
                    case Key.J:
                        if (!GalleryFunctions.IsGalleryOpen)
                        {
                            UpdateUIValues.ToggleQuickResize();
                        }

                        break;

                    // Delete, Shift + Delete
                    case Key.Delete:
                        if (!GalleryFunctions.IsGalleryOpen)
                        {
                            await DeleteFileAsync(!ShiftDown, Pics[FolderIndex]).ConfigureAwait(false);
                        }

                        break;

                    // Ctrl + C, Ctrl + Shift + C, Ctrl + Alt + C
                    case Key.C:
                        if (CtrlDown && !GalleryFunctions.IsGalleryOpen)
                        {
                            if (ShiftDown)
                            {
                                CopyBitmap();
                            }
                            else if (AltDown)
                            {
                                CopyFilePath();
                            }
                            else
                            {
                                if (GetMainWindow.MainImage.Effect != null)
                                    CopyBitmap();
                                else
                                    CopyFile();
                            }
                        }
                        else if (!GalleryFunctions.IsGalleryOpen)
                        {
                            CropFunctions.StartCrop();
                        }

                        break;

                    // Ctrl + V
                    case Key.V:
                        if (CtrlDown && !GalleryFunctions.IsGalleryOpen)
                        {
                            await PasteAsync().ConfigureAwait(false);
                        }

                        break;

                    case Key.I:
                        if (CtrlDown && !GalleryFunctions.IsGalleryOpen)
                        {
                            if (AltDown)
                            {
                                UpdateUIValues.ToggleQuickResize();
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
                        if (CtrlDown && !GalleryFunctions.IsGalleryOpen)
                        {
                            Print(Pics[FolderIndex]);
                        }

                        break;

                    //  R
                    case Key.R:
                        if (CtrlDown && !GalleryFunctions.IsGalleryOpen)
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
                        if (!GalleryFunctions.IsGalleryOpen)
                        {
                            OpenWith();
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
                        if (CtrlDown)
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
                        await GalleryToggle.ToggleGalleryAsync().ConfigureAwait(false);
                        break;

                    // Space
                    case Key.Space:
                        if (GetPicGallery != null)
                        {
                            if (GalleryFunctions.IsGalleryOpen)
                            {
                                GalleryNavigation.ScrollToGalleryCenter();
                                return;
                            }
                        }

                        WindowSizing.CenterWindowOnScreen();
                        break;

                    // 1
                    case Key.D1:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsGalleryOpen
                                                  || Settings.Default.Fullscreen)
                        {
                            break;
                        }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["AutoFitWindowMessage"]);
                        UpdateUIValues.SetScalingBehaviour(true, false);
                        break;

                    // 2
                    case Key.D2:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsGalleryOpen
                                                  || Settings.Default.Fullscreen)
                        {
                            break;
                        }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["AutoFitWindowFillHeight"]);
                        UpdateUIValues.SetScalingBehaviour(true, true);
                        break;

                    // 3
                    case Key.D3:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsGalleryOpen
                                                  || Settings.Default.Fullscreen)
                        {
                            break;
                        }

                        Tooltip.ShowTooltipMessage(Application.Current.Resources["NormalWindowBehavior"]);
                        UpdateUIValues.SetScalingBehaviour(false, false);
                        break;

                    // 4
                    case Key.D4:
                        if (QuickSettingsMenuOpen || GalleryFunctions.IsGalleryOpen
                                                  || Settings.Default.Fullscreen)
                        {
                            break;
                        }

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
                        OpenInExplorer(Pics[FolderIndex]);
                        break;

                    // F4
                    case Key.F4:
                        AllSettingsWindow();
                        break;

                    // F5
                    case Key.F5:
                        if (!GalleryFunctions.IsGalleryOpen)
                        {
                            Slideshow.StartSlideshow();
                        }

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

                    // F12
                    case Key.F12:
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
                        if (GalleryFunctions.IsGalleryOpen)
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
            if (GetMainWindow.TitleText.IsKeyboardFocusWithin)
            {
                return;
            }

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
            {
                if (e.SystemKey == Key.Z && !GalleryFunctions.IsGalleryOpen)
                {
                    HideInterfaceLogic.ToggleInterface();
                }
                else if (e.SystemKey == Key.Enter)
                {
                    WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen);
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