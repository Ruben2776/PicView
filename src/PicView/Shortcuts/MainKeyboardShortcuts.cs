using PicView.ChangeImage;
using PicView.Editing.Crop;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Windows.Input;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.UC;

namespace PicView.Shortcuts
{
    internal static class MainKeyboardShortcuts
    {
        /// <summary>
        /// Indicates whether a key is held down.
        /// </summary>
        internal static bool IsKeyHeldDown { get; private set; }

        /// <summary>
        /// Indicates whether the Ctrl key is pressed.
        /// </summary>
        internal static bool CtrlDown { get; private set; }

        /// <summary>
        /// Indicates whether the Alt key is pressed.
        /// </summary>
        internal static bool AltDown { get; private set; }

        /// <summary>
        /// Indicates whether the Shift key is pressed.
        /// </summary>
        internal static bool ShiftDown { get; private set; }

        /// <summary>
        /// Gets the currently pressed key.
        /// </summary>
        internal static Key CurrentKey { get; private set; }

        /// <summary>
        /// Handles keydown events for the main window.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Key event arguments.</param>
        internal static async Task MainWindow_KeysDownAsync(object sender, KeyEventArgs e)
        {
            #region return statements

            // Don't allow keys when typing in text
            if (GetMainWindow.TitleText.IsKeyboardFocusWithin)
            {
                return;
            }

            // Don't execute keys when typing in GoToPicBox or in QuickResize
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

            await GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                CtrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                AltDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
                ShiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            });

            #region CroppingKeys

            if (GetCroppingTool is { IsVisible: true })
            {
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
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
            CurrentKey = e.Key;
            IsKeyHeldDown = e.IsRepeat;

            if (CustomKeybindings.CustomShortcuts is null)
            {
                return;
            }

            if (CustomKeybindings.CustomShortcuts.TryGetValue(CurrentKey, out var shortcut))
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (shortcut is null)
                {
                    var backup = false;
                    try
                    {
                        if (ErrorHandling.CheckOutOfRange())
                        {
                            Navigation.BackupPath = Navigation.Pics[Navigation.FolderIndex];
                            backup = true;
                        }

                        await CustomKeybindings.CreateNewDefaultKeybindingFile().ConfigureAwait(false);
                        // ReSharper disable once TailRecursiveCall
                        await MainWindow_KeysDownAsync(sender, e).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        await ErrorHandling.ReloadAsync(fromBackup: backup).ConfigureAwait(false);
                    }
                    return;
                }
                // Execute the associated action
                await shortcut.Invoke().ConfigureAwait(false);
            }
            else
            {
                await UIHelper.CheckModifierFunctionAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handles keyup events for the main window.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Key event arguments.</param>
        internal static async Task MainWindow_KeysUp(object sender, KeyEventArgs e)
        {
            #region return statements

            // Don't allow keys when typing in text
            if (GetMainWindow.TitleText.IsKeyboardFocusWithin)
            {
                return;
            }

            // Don't execute keys when typing in GoToPicBox or in QuickResize
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

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
            {
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (e.SystemKey == Key.Z && !GalleryFunctions.IsGalleryOpen)
                {
                    await GetMainWindow.Dispatcher.InvokeAsync(HideInterfaceLogic.ToggleInterface);
                }
                else if (e.SystemKey == Key.Enter)
                {
                    await GetMainWindow.Dispatcher.InvokeAsync(() =>
                    {
                        WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen);
                    });
                }

                AltDown = false;
                return;
            }

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (e.Key)
            {
                case Key.A:
                case Key.Right:
                case Key.Left:
                case Key.D:
                    if (Navigation.FolderIndex < 0 || Navigation.FolderIndex >= Navigation.Pics.Count)
                        return;
                    await FastPic.FastPicUpdateAsync().ConfigureAwait(false);
                    return;

                case Key.LeftShift:
                case Key.RightShift:
                    ShiftDown = false;
                    return;

                case Key.LeftCtrl:
                case Key.RightCtrl:
                    CtrlDown = false;
                    return;
            }
        }
    }
}