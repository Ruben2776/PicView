using PicView.ChangeImage;
using PicView.Editing.Crop;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.UC;

namespace PicView.Shortcuts
{
    internal static class MainKeyboardShortcuts
    {
        internal static bool IsKeyHeldDown { get; private set; }
        internal static bool CtrlDown { get; private set; }
        internal static bool AltDown { get; private set; }
        internal static bool ShiftDown { get; private set; }
        internal static Key CurrentKey { get; private set; }

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
            CurrentKey = e.Key;
            IsKeyHeldDown = e.IsRepeat;

            if (CustomKeybindings.CustomShortcuts.TryGetValue(CurrentKey, out var shortcut))
            {
                // Execute the associated action
                await shortcut.Invoke().ConfigureAwait(false);
            }
            else
            {
                await UIHelper.CheckModifierFunctionAsync().ConfigureAwait(false);
            }
        }

        internal static async Task MainWindow_KeysUp(object sender, KeyEventArgs e)
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
                    await FastPic.FastPicUpdateAsync().ConfigureAwait(false);
                    return;
            }
        }
    }
}