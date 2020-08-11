using PicView.UILogic.Animations;
using PicView.UILogic.PicGallery;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.Open_Save;
using static PicView.Shortcuts.MainShortcuts;
using static PicView.UILogic.Animations.MouseOverAnimations;
using static PicView.UILogic.DragAndDrop.Image_DragAndDrop;
using static PicView.UILogic.HideInterfaceLogic;
using static PicView.UILogic.Sizing.WindowLogic;
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
            LoadWindows.GetMainWindow.KeyDown += MainWindow_KeysDown;
            LoadWindows.GetMainWindow.KeyUp += MainWindow_KeysUp;
            LoadWindows.GetMainWindow.MouseDown += MainWindow_MouseDown;

            // MinButton
            LoadWindows.GetMainWindow.MinButton.TheButton.Click += (s, x) => SystemCommands.MinimizeWindow(LoadWindows.GetMainWindow);

            // CloseButton
            LoadWindows.GetMainWindow.CloseButton.TheButton.Click += (s, x) => SystemCommands.CloseWindow(LoadWindows.GetMainWindow);

            // FileMenuButton
            LoadWindows.GetMainWindow.FileMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(LoadWindows.GetMainWindow.FolderFill);
            LoadWindows.GetMainWindow.FileMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(LoadWindows.GetMainWindow.FolderFill);
            LoadWindows.GetMainWindow.FileMenuButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(LoadWindows.GetMainWindow.FileMenuBg);
            LoadWindows.GetMainWindow.FileMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(LoadWindows.GetMainWindow.FolderFill);
            LoadWindows.GetMainWindow.FileMenuButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(LoadWindows.GetMainWindow.FileMenuBg);
            LoadWindows.GetMainWindow.FileMenuButton.Click += Toggle_open_menu;

            GetFileMenu.Open.Click += (s, x) => Open();
            GetFileMenu.FileLocation.Click += (s, x) => Open_In_Explorer();
            GetFileMenu.Print.Click += (s, x) => Print(Pics[FolderIndex]);
            GetFileMenu.SaveButton.Click += (s, x) => SaveFiles();

            GetFileMenu.OpenBorder.MouseLeftButtonUp += (s, x) => Open();
            GetFileMenu.FileLocationBorder.MouseLeftButtonUp += (s, x) => Open_In_Explorer();
            GetFileMenu.PrintBorder.MouseLeftButtonUp += (s, x) => Print(Pics[FolderIndex]);
            GetFileMenu.SaveBorder.MouseLeftButtonUp += (s, x) => SaveFiles();

            // image_button
            LoadWindows.GetMainWindow.image_button.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(LoadWindows.GetMainWindow.ImagePath1Fill, LoadWindows.GetMainWindow.ImagePath2Fill, LoadWindows.GetMainWindow.ImagePath3Fill);
            LoadWindows.GetMainWindow.image_button.MouseEnter += (s, x) => ButtonMouseOverAnim(LoadWindows.GetMainWindow.ImagePath1Fill, LoadWindows.GetMainWindow.ImagePath2Fill, LoadWindows.GetMainWindow.ImagePath3Fill);
            LoadWindows.GetMainWindow.image_button.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(LoadWindows.GetMainWindow.ImageMenuBg);
            LoadWindows.GetMainWindow.image_button.MouseLeave += (s, x) => ButtonMouseLeaveAnim(LoadWindows.GetMainWindow.ImagePath1Fill, LoadWindows.GetMainWindow.ImagePath2Fill, LoadWindows.GetMainWindow.ImagePath3Fill);
            LoadWindows.GetMainWindow.image_button.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(LoadWindows.GetMainWindow.ImageMenuBg);
            LoadWindows.GetMainWindow.image_button.Click += Toggle_image_menu;

            // imageSettingsMenu Buttons

            GetImageSettingsMenu.Contained_Gallery.Click += delegate
            {
                Close_UserControls();
                GalleryToggle.OpenContainedGallery();
            };
            GetImageSettingsMenu.Fullscreen_Gallery.Click += delegate
            {
                Close_UserControls();
                GalleryToggle.OpenFullscreenGallery();
            };

            // SettingsButton
            LoadWindows.GetMainWindow.SettingsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(LoadWindows.GetMainWindow.SettingsButtonFill);
            LoadWindows.GetMainWindow.SettingsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(LoadWindows.GetMainWindow.SettingsButtonFill);
            LoadWindows.GetMainWindow.SettingsButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(LoadWindows.GetMainWindow.SettingsMenuBg);
            LoadWindows.GetMainWindow.SettingsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(LoadWindows.GetMainWindow.SettingsButtonFill);
            LoadWindows.GetMainWindow.SettingsButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(LoadWindows.GetMainWindow.SettingsMenuBg);
            LoadWindows.GetMainWindow.SettingsButton.Click += Toggle_quick_settings_menu;

            //FunctionButton
            var MagicBrush = LoadWindows.GetMainWindow.TryFindResource("MagicBrush") as SolidColorBrush;
            LoadWindows.GetMainWindow.FunctionMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(MagicBrush);
            LoadWindows.GetMainWindow.FunctionMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(MagicBrush);
            LoadWindows.GetMainWindow.FunctionMenuButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(LoadWindows.GetMainWindow.EffectsMenuBg);
            LoadWindows.GetMainWindow.FunctionMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(MagicBrush);
            LoadWindows.GetMainWindow.FunctionMenuButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(LoadWindows.GetMainWindow.EffectsMenuBg);
            LoadWindows.GetMainWindow.FunctionMenuButton.Click += Toggle_Functions_menu;

            // ClickArrows
            GetClickArrowLeft.MouseLeftButtonDown += (s, x) => PicButton(true, false);

            GetClickArrowRight.MouseLeftButtonDown += (s, x) => PicButton(true, true);

            // x2
            Getx2.MouseLeftButtonDown += (x, xx) => SystemCommands.CloseWindow(LoadWindows.GetMainWindow);

            // Minus
            GetMinus.MouseLeftButtonDown += (s, x) => SystemCommands.MinimizeWindow(LoadWindows.GetMainWindow);

            // GalleryShortcut
            GetGalleryShortcut.MouseLeftButtonDown += (s, x) => GalleryToggle.OpenContainedGallery();

            // Bar
            LoadWindows.GetMainWindow.TitleText.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
            LoadWindows.GetMainWindow.TitleText.InnerTextBox.PreviewKeyDown += CustomTextBoxShortcuts.CustomTextBox_KeyDown;
            LoadWindows.GetMainWindow.TitleText.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;
            LoadWindows.GetMainWindow.TitleText.PreviewMouseRightButtonDown += EditTitleBar.Bar_PreviewMouseRightButtonDown;

            // img
            LoadWindows.GetMainWindow.MainImage.PreviewMouseLeftButtonDown += DragAndDrop.DragToExplorer.DragFile;
            LoadWindows.GetMainWindow.MainImage.MouseLeftButtonDown += MainImage_MouseLeftButtonDown;
            LoadWindows.GetMainWindow.MainImage.MouseLeftButtonUp += MainImage_MouseLeftButtonUp;
            LoadWindows.GetMainWindow.MainImage.MouseMove += MainImage_MouseMove;
            LoadWindows.GetMainWindow.MainImage.MouseWheel += MainImage_MouseWheel;

            // bg
            LoadWindows.GetMainWindow.ParentContainer.MouseLeftButtonDown += Bg_MouseLeftButtonDown;
            LoadWindows.GetMainWindow.ParentContainer.Drop += Image_Drop;
            LoadWindows.GetMainWindow.ParentContainer.DragEnter += Image_DragEnter;
            LoadWindows.GetMainWindow.ParentContainer.DragLeave += Image_DragLeave;
            LoadWindows.GetMainWindow.ParentContainer.MouseMove += Interface_MouseMove;
            LoadWindows.GetMainWindow.ParentContainer.MouseLeave += Interface_MouseLeave;

            // TooltipStyle
            GetToolTipMessage.MouseWheel += MainImage_MouseWheel;

            // TitleBar
            LoadWindows.GetMainWindow.TitleBar.MouseLeftButtonDown += Move;
            LoadWindows.GetMainWindow.TitleBar.MouseLeave += Restore_From_Move;

            // Lower Bar
            LoadWindows.GetMainWindow.LowerBar.Drop += Image_Drop;
            LoadWindows.GetMainWindow.LowerBar.MouseLeftButtonDown += MoveAlt;

            // This
            LoadWindows.GetMainWindow.Closing += Window_Closing;
            LoadWindows.GetMainWindow.StateChanged += MainWindow_StateChanged;

            //LocationChanged += MainWindow_LocationChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

#if DEBUG
            Trace.WriteLine("Events loaded");
#endif
        }
    }
}