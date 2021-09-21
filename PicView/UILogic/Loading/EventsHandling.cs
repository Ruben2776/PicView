using PicView.PicGallery;
using PicView.UILogic.Animations;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.Open_Save;
using static PicView.Shortcuts.MainShortcuts;
using static PicView.UILogic.Animations.MouseOverAnimations;
using static PicView.UILogic.DragAndDrop.Image_DragAndDrop;
using static PicView.UILogic.HideInterfaceLogic;
using static PicView.UILogic.UC;

namespace PicView.UILogic.Loading
{
    internal static class Eventshandling
    {
        /// <summary>
        /// Start adding events
        /// </summary>
        internal static void Go()
        {
            // keyboard and Mouse_Keys Keys
            ConfigureWindows.GetMainWindow.KeyDown += async (sender, e) => await MainWindow_KeysDownAsync(sender, e).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.KeyUp += async (sender, e) => await MainWindow_KeysUpAsync(sender, e).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.MouseDown += async (sender, e) => await MainWindow_MouseDownAsync(sender, e).ConfigureAwait(false);

            // MinButton
            ConfigureWindows.GetMainWindow.MinButton.TheButton.Click += (_, _) => SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow);

            // CloseButton
            ConfigureWindows.GetMainWindow.CloseButton.TheButton.Click += (_, _) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

            // FileMenuButton
            ConfigureWindows.GetMainWindow.FileMenuButton.PreviewMouseLeftButtonDown += (_, _) => PreviewMouseButtonDownAnim(ConfigureWindows.GetMainWindow.FolderFill);
            ConfigureWindows.GetMainWindow.FileMenuButton.MouseEnter += (_, _) => ButtonMouseOverAnim(ConfigureWindows.GetMainWindow.FolderFill);
            ConfigureWindows.GetMainWindow.FileMenuButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.FileMenuBg);
            ConfigureWindows.GetMainWindow.FileMenuButton.MouseLeave += (_, _) => ButtonMouseLeaveAnim(ConfigureWindows.GetMainWindow.FolderFill);
            ConfigureWindows.GetMainWindow.FileMenuButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.FileMenuBg);
            ConfigureWindows.GetMainWindow.FileMenuButton.Click += Toggle_open_menu;

            GetFileMenu.Open.Click += async (_, _) => await OpenAsync().ConfigureAwait(false);
            GetFileMenu.FileLocation.Click += (_, _) => Open_In_Explorer();
            GetFileMenu.Print.Click += (_, _) => Print(Pics?[FolderIndex]);
            GetFileMenu.SaveButton.Click += async (sender, e) => await SaveFilesAsync();

            GetFileMenu.OpenBorder.MouseLeftButtonUp += async (_, _) => await OpenAsync().ConfigureAwait(false);
            GetFileMenu.FileLocationBorder.MouseLeftButtonUp += (_, _) => Open_In_Explorer();
            GetFileMenu.PrintBorder.MouseLeftButtonUp += (_, _) => Print(Pics?[FolderIndex]);
            GetFileMenu.SaveBorder.MouseLeftButtonUp += async (sender, e) => await SaveFilesAsync();

            // image_button
            ConfigureWindows.GetMainWindow.image_button.PreviewMouseLeftButtonDown += (_, _) => PreviewMouseButtonDownAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseEnter += (_, _) => ButtonMouseOverAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.ImageMenuBg);
            ConfigureWindows.GetMainWindow.image_button.MouseLeave += (_, _) => ButtonMouseLeaveAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.ImageMenuBg);
            ConfigureWindows.GetMainWindow.image_button.Click += Toggle_image_menu;

            // imageSettingsMenu Buttons

            GetImageSettingsMenu.Contained_Gallery.Click += async delegate
            {
                Close_UserControls();
                await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
            };
            GetImageSettingsMenu.Fullscreen_Gallery.Click += async delegate
            {
                Close_UserControls();
                await GalleryToggle.OpenFullscreenGalleryAsync().ConfigureAwait(false);
            };

            // SettingsButton
            ConfigureWindows.GetMainWindow.SettingsButton.PreviewMouseLeftButtonDown += (_, _) => PreviewMouseButtonDownAnim(ConfigureWindows.GetMainWindow.SettingsButtonFill);
            ConfigureWindows.GetMainWindow.SettingsButton.MouseEnter += (_, _) => ButtonMouseOverAnim(ConfigureWindows.GetMainWindow.SettingsButtonFill);
            ConfigureWindows.GetMainWindow.SettingsButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.SettingsMenuBg);
            ConfigureWindows.GetMainWindow.SettingsButton.MouseLeave += (_, _) => ButtonMouseLeaveAnim(ConfigureWindows.GetMainWindow.SettingsButtonFill);
            ConfigureWindows.GetMainWindow.SettingsButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.SettingsMenuBg);
            ConfigureWindows.GetMainWindow.SettingsButton.Click += Toggle_quick_settings_menu;

            //FunctionButton
            var MagicBrush = ConfigureWindows.GetMainWindow.TryFindResource("MagicBrush") as SolidColorBrush;
            ConfigureWindows.GetMainWindow.FunctionMenuButton.PreviewMouseLeftButtonDown += (_, _) => PreviewMouseButtonDownAnim(MagicBrush);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.MouseEnter += (_, _) => ButtonMouseOverAnim(MagicBrush);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.EffectsMenuBg);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.MouseLeave += (_, _) => ButtonMouseLeaveAnim(MagicBrush);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.EffectsMenuBg);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.Click += Toggle_Functions_menu;

            // ClickArrows
            GetClickArrowLeft.MouseLeftButtonDown += async (_, _) => await PicButtonAsync(true, false).ConfigureAwait(false);

            GetClickArrowRight.MouseLeftButtonDown += async (_, _) => await PicButtonAsync(true, true).ConfigureAwait(false);

            // x2
            Getx2.MouseLeftButtonDown += (_, _) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

            // Minus
            GetMinus.MouseLeftButtonDown += (_, _) => SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow);

            // GalleryShortcut
            GetGalleryShortcut.MouseLeftButtonDown += async (_, _) => await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);

            // TitleText
            ConfigureWindows.GetMainWindow.TitleText.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
            ConfigureWindows.GetMainWindow.TitleText.InnerTextBox.PreviewKeyDown += async (sender, e) => await CustomTextBoxShortcuts.CustomTextBox_KeyDownAsync(sender, e).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.TitleText.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;
            ConfigureWindows.GetMainWindow.TitleText.PreviewMouseRightButtonDown += EditTitleBar.Bar_PreviewMouseRightButtonDown;

            // MainImage
            ConfigureWindows.GetMainWindow.MainImage.PreviewMouseLeftButtonDown += DragAndDrop.DragToExplorer.DragFile;
            ConfigureWindows.GetMainWindow.MainImage.MouseLeftButtonDown += MainImage_MouseLeftButtonDown;
            ConfigureWindows.GetMainWindow.MainImage.MouseLeftButtonUp += MainImage_MouseLeftButtonUp;
            ConfigureWindows.GetMainWindow.MainImage.MouseMove += MainImage_MouseMove;

            // ParentContainer
            ConfigureWindows.GetMainWindow.ParentContainer.MouseLeftButtonDown += async (sender, e) => await Bg_MouseLeftButtonDownAsync(sender, e).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.ParentContainer.Drop += async (sender, e) => await Image_Drop(sender, e).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.ParentContainer.DragEnter += Image_DragEnter;
            ConfigureWindows.GetMainWindow.ParentContainer.DragLeave += Image_DragLeave;
            ConfigureWindows.GetMainWindow.ParentContainer.MouseMove += async (sender, e) => await Interface_MouseMoveAsync(sender, e).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.ParentContainer.MouseLeave += async (sender, e) => await Interface_MouseLeaveAsync(sender, e).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.ParentContainer.PreviewMouseWheel += async (sender, e) => await MainImage_MouseWheelAsync(sender, e).ConfigureAwait(false);

            // TooltipStyle
            GetToolTipMessage.MouseWheel += async (sender, e) => await MainImage_MouseWheelAsync(sender, e).ConfigureAwait(false);

            // TitleBar
            ConfigureWindows.GetMainWindow.TitleBar.MouseLeftButtonDown += ConfigureWindows.Move;
            ConfigureWindows.GetMainWindow.TitleBar.MouseLeave += ConfigureWindows.Restore_From_Move;

            // Lower Bar
            ConfigureWindows.GetMainWindow.LowerBar.Drop += async (sender, e) => await Image_Drop(sender, e).ConfigureAwait(false);
            ConfigureWindows.GetMainWindow.LowerBar.MouseLeftButtonDown += ConfigureWindows.MoveAlt;

            // This
            ConfigureWindows.GetMainWindow.Closing += ConfigureWindows.Window_Closing;
            ConfigureWindows.GetMainWindow.StateChanged += ConfigureWindows.MainWindow_StateChanged;

            //LocationChanged += MainWindow_LocationChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += ConfigureWindows.SystemEvents_DisplaySettingsChanged;

#if DEBUG
            Trace.WriteLine("Events loaded");
#endif
        }
    }
}