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

            GetFileMenu.Open.Click += (_, _) => Open();
            GetFileMenu.FileLocation.Click += (_, _) => Open_In_Explorer();
            GetFileMenu.Print.Click += (_, _) => Print(Pics[FolderIndex]);
            GetFileMenu.SaveButton.Click += (_, _) => SaveFiles();

            GetFileMenu.OpenBorder.MouseLeftButtonUp += (_, _) => Open();
            GetFileMenu.FileLocationBorder.MouseLeftButtonUp += (_, _) => Open_In_Explorer();
            GetFileMenu.PrintBorder.MouseLeftButtonUp += (_, _) => Print(Pics[FolderIndex]);
            GetFileMenu.SaveBorder.MouseLeftButtonUp += (_, _) => SaveFiles();

            // image_button
            ConfigureWindows.GetMainWindow.image_button.PreviewMouseLeftButtonDown += (_, _) => PreviewMouseButtonDownAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseEnter += (_, _) => ButtonMouseOverAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ConfigureWindows.GetMainWindow.ImageMenuBg);
            ConfigureWindows.GetMainWindow.image_button.MouseLeave += (_, _) => ButtonMouseLeaveAnim(ConfigureWindows.GetMainWindow.ImagePath1Fill, ConfigureWindows.GetMainWindow.ImagePath2Fill, ConfigureWindows.GetMainWindow.ImagePath3Fill);
            ConfigureWindows.GetMainWindow.image_button.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ConfigureWindows.GetMainWindow.ImageMenuBg);
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
            GetClickArrowLeft.MouseLeftButtonDown += (_, _) => PicButton(true, false);

            GetClickArrowRight.MouseLeftButtonDown += (_, _) => PicButton(true, true);

            // x2
            Getx2.MouseLeftButtonDown += (_, _) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

            // Minus
            GetMinus.MouseLeftButtonDown += (_, _) => SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow);

            // GalleryShortcut
            GetGalleryShortcut.MouseLeftButtonDown += (_, _) => GalleryToggle.OpenHorizontalGallery();

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