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
            ConfigureWindows.GetMainWindow.KeyDown += MainWindow_KeysDown;
            ConfigureWindows.GetMainWindow.KeyUp += MainWindow_KeysUp;
            ConfigureWindows.GetMainWindow.MouseDown += MainWindow_MouseDown;

            // MinButton
            ConfigureWindows.GetMainWindow.MinButton.TheButton.Click += (s, x) => SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow);

            // CloseButton
            ConfigureWindows.GetMainWindow.CloseButton.TheButton.Click += (s, x) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

            // FileMenuButton
            ConfigureWindows.GetMainWindow.FileMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ConfigureWindows.GetMainWindow.FolderFill);
            ConfigureWindows.GetMainWindow.FileMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ConfigureWindows.GetMainWindow.FolderFill);
            ConfigureWindows.GetMainWindow.FileMenuButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.FileMenuBg);
            ConfigureWindows.GetMainWindow.FileMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(ConfigureWindows.GetMainWindow.FolderFill);
            ConfigureWindows.GetMainWindow.FileMenuButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.FileMenuBg);
            ConfigureWindows.GetMainWindow.FileMenuButton.Click += Toggle_open_menu;

            GetFileMenu.Open.Click += (s, x) => Open();
            GetFileMenu.FileLocation.Click += (s, x) => Open_In_Explorer();
            GetFileMenu.Print.Click += (s, x) => Print(Pics[FolderIndex]);
            GetFileMenu.SaveButton.Click += (s, x) => SaveFiles();

            GetFileMenu.OpenBorder.MouseLeftButtonUp += (s, x) => Open();
            GetFileMenu.FileLocationBorder.MouseLeftButtonUp += (s, x) => Open_In_Explorer();
            GetFileMenu.PrintBorder.MouseLeftButtonUp += (s, x) => Print(Pics[FolderIndex]);
            GetFileMenu.SaveBorder.MouseLeftButtonUp += (s, x) => SaveFiles();

            // image_button
            ConfigureWindows.GetMainWindow.image_button.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseEnter += (s, x) => ButtonMouseOverAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.ImageMenuBg);
            ConfigureWindows.GetMainWindow.image_button.MouseLeave += (s, x) => ButtonMouseLeaveAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.ImageMenuBg);
            ConfigureWindows.GetMainWindow.image_button.Click += Toggle_image_menu;

            // imageSettingsMenu Buttons

            GetImageSettingsMenu.Contained_Gallery.Click += delegate
            {
                Close_UserControls();
                GalleryToggle.OpenHorizontalGallery();
            };
            GetImageSettingsMenu.Fullscreen_Gallery.Click += delegate
            {
                Close_UserControls();
                GalleryToggle.OpenFullscreenGallery();
            };

            // SettingsButton
            ConfigureWindows.GetMainWindow.SettingsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ConfigureWindows.GetMainWindow.SettingsButtonFill);
            ConfigureWindows.GetMainWindow.SettingsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ConfigureWindows.GetMainWindow.SettingsButtonFill);
            ConfigureWindows.GetMainWindow.SettingsButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.SettingsMenuBg);
            ConfigureWindows.GetMainWindow.SettingsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(ConfigureWindows.GetMainWindow.SettingsButtonFill);
            ConfigureWindows.GetMainWindow.SettingsButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.SettingsMenuBg);
            ConfigureWindows.GetMainWindow.SettingsButton.Click += Toggle_quick_settings_menu;

            //FunctionButton
            var MagicBrush = ConfigureWindows.GetMainWindow.TryFindResource("MagicBrush") as SolidColorBrush;
            ConfigureWindows.GetMainWindow.FunctionMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(MagicBrush);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(MagicBrush);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.EffectsMenuBg);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(MagicBrush);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.EffectsMenuBg);
            ConfigureWindows.GetMainWindow.FunctionMenuButton.Click += Toggle_Functions_menu;

            // ClickArrows
            GetClickArrowLeft.MouseLeftButtonDown += (s, x) => PicButton(true, false);

            GetClickArrowRight.MouseLeftButtonDown += (s, x) => PicButton(true, true);

            // x2
            Getx2.MouseLeftButtonDown += (x, xx) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

            // Minus
            GetMinus.MouseLeftButtonDown += (s, x) => SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow);

            // GalleryShortcut
            GetGalleryShortcut.MouseLeftButtonDown += (s, x) => GalleryToggle.OpenHorizontalGallery();

            // TitleText
            ConfigureWindows.GetMainWindow.TitleText.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
            ConfigureWindows.GetMainWindow.TitleText.InnerTextBox.PreviewKeyDown += CustomTextBoxShortcuts.CustomTextBox_KeyDown;
            ConfigureWindows.GetMainWindow.TitleText.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;
            ConfigureWindows.GetMainWindow.TitleText.PreviewMouseRightButtonDown += EditTitleBar.Bar_PreviewMouseRightButtonDown;

            // MainImage
            ConfigureWindows.GetMainWindow.MainImage.PreviewMouseLeftButtonDown += DragAndDrop.DragToExplorer.DragFile;
            ConfigureWindows.GetMainWindow.MainImage.MouseLeftButtonDown += MainImage_MouseLeftButtonDown;
            ConfigureWindows.GetMainWindow.MainImage.MouseLeftButtonUp += MainImage_MouseLeftButtonUp;
            ConfigureWindows.GetMainWindow.MainImage.MouseMove += MainImage_MouseMove;


            // ParentContainer
            ConfigureWindows.GetMainWindow.ParentContainer.MouseLeftButtonDown += Bg_MouseLeftButtonDown;
            ConfigureWindows.GetMainWindow.ParentContainer.Drop += Image_Drop;
            ConfigureWindows.GetMainWindow.ParentContainer.DragEnter += Image_DragEnter;
            ConfigureWindows.GetMainWindow.ParentContainer.DragLeave += Image_DragLeave;
            ConfigureWindows.GetMainWindow.ParentContainer.MouseMove += Interface_MouseMove;
            ConfigureWindows.GetMainWindow.ParentContainer.MouseLeave += Interface_MouseLeave;
            ConfigureWindows.GetMainWindow.ParentContainer.PreviewMouseWheel += MainImage_MouseWheel;

            // TooltipStyle
            GetToolTipMessage.MouseWheel += MainImage_MouseWheel;

            // TitleBar
            ConfigureWindows.GetMainWindow.TitleBar.MouseLeftButtonDown += ConfigureWindows.Move;
            ConfigureWindows.GetMainWindow.TitleBar.MouseLeave += ConfigureWindows.Restore_From_Move;

            // Lower Bar
            ConfigureWindows.GetMainWindow.LowerBar.Drop += Image_Drop;
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