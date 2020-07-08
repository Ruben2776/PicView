using PicView.FileHandling;
using PicView.SystemIntegration;
using PicView.UI.Animations;
using PicView.UI.PicGallery;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.Copy_Paste;
using static PicView.FileHandling.DeleteFiles;
using static PicView.FileHandling.Open_Save;
using static PicView.Library.Fields;
using static PicView.Shortcuts.MainShortcuts;
using static PicView.UI.Animations.MouseOverAnimations;
using static PicView.UI.DragAndDrop.Image_DragAndDrop;
using static PicView.UI.HideInterfaceLogic;
using static PicView.UI.Sizing.ScaleImage;
using static PicView.UI.Sizing.WindowLogic;
using static PicView.UI.TransformImage.Rotation;
using static PicView.UI.TransformImage.ZoomLogic;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.Loading
{
    internal static class Eventshandling
    {
        /// <summary>
        /// Start adding events
        /// </summary>
        internal static void Go()
        {
            // keyboard and Mouse_Keys Keys
            TheMainWindow.KeyDown += MainWindow_KeysDown;
            TheMainWindow.KeyUp += MainWindow_KeysUp;
            TheMainWindow.MouseDown += MainWindow_MouseDown;

            // MinButton
            TheMainWindow.MinButton.TheButton.Click += (s, x) => SystemCommands.MinimizeWindow(TheMainWindow);

            // MaxButton
            TheMainWindow.FullscreenButton.TheButton.Click += (s, x) => Fullscreen_Restore();

            // CloseButton
            TheMainWindow.CloseButton.TheButton.Click += (s, x) => SystemCommands.CloseWindow(TheMainWindow);

            // FileMenuButton
            TheMainWindow.FileMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(TheMainWindow.FolderFill);
            TheMainWindow.FileMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(TheMainWindow.FolderFill);
            TheMainWindow.FileMenuButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(TheMainWindow.FileMenuBg);
            TheMainWindow.FileMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(TheMainWindow.FolderFill);
            TheMainWindow.FileMenuButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(TheMainWindow.FileMenuBg);
            TheMainWindow.FileMenuButton.Click += Toggle_open_menu;

            GetFileMenu.Open.Click += (s, x) => Open();
            GetFileMenu.FileLocation.Click += (s, x) => Open_In_Explorer();
            GetFileMenu.Print.Click += (s, x) => Print(Pics[FolderIndex]);
            GetFileMenu.SaveBorder.Click += (s, x) => SaveFiles();

            GetFileMenu.OpenBorder.MouseLeftButtonUp += (s, x) => Open();
            GetFileMenu.FileLocationBorder.MouseLeftButtonUp += (s, x) => Open_In_Explorer();
            GetFileMenu.PrintBorder.MouseLeftButtonUp += (s, x) => Print(Pics[FolderIndex]);
            GetFileMenu.Save_File_Location_Border.MouseLeftButtonUp += (s, x) => SaveFiles();

            // image_button
            TheMainWindow.image_button.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(TheMainWindow.ImagePath1Fill, TheMainWindow.ImagePath2Fill, TheMainWindow.ImagePath3Fill);
            TheMainWindow.image_button.MouseEnter += (s, x) => ButtonMouseOverAnim(TheMainWindow.ImagePath1Fill, TheMainWindow.ImagePath2Fill, TheMainWindow.ImagePath3Fill);
            TheMainWindow.image_button.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(TheMainWindow.ImageMenuBg);
            TheMainWindow.image_button.MouseLeave += (s, x) => ButtonMouseLeaveAnim(TheMainWindow.ImagePath1Fill, TheMainWindow.ImagePath2Fill, TheMainWindow.ImagePath3Fill);
            TheMainWindow.image_button.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(TheMainWindow.ImageMenuBg);
            TheMainWindow.image_button.Click += Toggle_image_menu;

            // imageSettingsMenu Buttons
            GetImageSettingsMenu.RotateRightButton.Click += (s, x) => Rotate(true);
            GetImageSettingsMenu.RotateLeftButton.Click += (s, x) => Rotate(false);

            GetImageSettingsMenu.Contained_Gallery.Click += delegate {
                Close_UserControls();
                GalleryToggle.OpenContainedGallery();
            };
            GetImageSettingsMenu.Fullscreen_Gallery.Click += delegate {
                Close_UserControls();
                GalleryToggle.OpenFullscreenGallery();
            };
            GetImageSettingsMenu.SlideshowButton.Click += delegate { Slideshow.StartSlideshow(); };

            // LeftButton
            TheMainWindow.LeftButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(TheMainWindow.LeftArrowFill);
            TheMainWindow.LeftButton.MouseEnter += (s, x) => ButtonMouseOverAnim(TheMainWindow.LeftArrowFill);
            TheMainWindow.LeftButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(TheMainWindow.LeftButtonBrush);
            TheMainWindow.LeftButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(TheMainWindow.LeftArrowFill);
            TheMainWindow.LeftButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(TheMainWindow.LeftButtonBrush);
            TheMainWindow.LeftButton.Click += (s, x) => PicButton(false, false);

            // RightButton
            TheMainWindow.RightButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(TheMainWindow.RightArrowFill);
            TheMainWindow.RightButton.MouseEnter += (s, x) => ButtonMouseOverAnim(TheMainWindow.RightArrowFill);
            TheMainWindow.RightButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(TheMainWindow.RightButtonBrush);
            TheMainWindow.RightButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(TheMainWindow.RightArrowFill);
            TheMainWindow.RightButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(TheMainWindow.RightButtonBrush);
            TheMainWindow.RightButton.Click += (s, x) => PicButton(false, true);

            // SettingsButton
            TheMainWindow.SettingsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(TheMainWindow.SettingsButtonFill);
            TheMainWindow.SettingsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(TheMainWindow.SettingsButtonFill);
            TheMainWindow.SettingsButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(TheMainWindow.SettingsMenuBg);
            TheMainWindow.SettingsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(TheMainWindow.SettingsButtonFill);
            TheMainWindow.SettingsButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(TheMainWindow.SettingsMenuBg);
            TheMainWindow.SettingsButton.Click += Toggle_quick_settings_menu;

            //FunctionButton
            var MagicBrush = TheMainWindow.TryFindResource("MagicBrush") as SolidColorBrush;
            TheMainWindow.FunctionMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(MagicBrush);
            TheMainWindow.FunctionMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(MagicBrush);
            TheMainWindow.FunctionMenuButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(TheMainWindow.EffectsMenuBg);
            TheMainWindow.FunctionMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(MagicBrush);
            TheMainWindow.FunctionMenuButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(TheMainWindow.EffectsMenuBg);
            TheMainWindow.FunctionMenuButton.Click += Toggle_Functions_menu;

            // FlipButton
            GetImageSettingsMenu.FlipButton.Click += (s, x) => Flip();

            // ClickArrows
            GetClickArrowLeft.MouseLeftButtonDown += (s, x) => PicButton(true, false);
            GetClickArrowLeft.MouseEnter += Interface_MouseEnter_Negative;

            GetClickArrowRight.MouseLeftButtonDown += (s, x) => PicButton(true, true);
            GetClickArrowRight.MouseEnter += Interface_MouseEnter_Negative;

            // x2
            Getx2.MouseLeftButtonDown += (x, xx) => SystemCommands.CloseWindow(TheMainWindow);
            Getx2.MouseEnter += Interface_MouseEnter_Negative;

            // Minus
            GetMinus.MouseLeftButtonDown += (s, x) => SystemCommands.MinimizeWindow(TheMainWindow);
            GetMinus.MouseEnter += Interface_MouseEnter_Negative;

            // GalleryShortcut
            GetGalleryShortcut.MouseLeftButtonDown += (s, x) => GalleryToggle.Toggle();
            GetGalleryShortcut.MouseEnter += Interface_MouseEnter_Negative;

            // Bar
            TheMainWindow.TitleText.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
            TheMainWindow.TitleText.InnerTextBox.PreviewKeyDown += CustomTextBoxShortcuts.CustomTextBox_KeyDown;
            TheMainWindow.TitleText.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;
            TheMainWindow.TitleText.PreviewMouseRightButtonDown += EditTitleBar.Bar_PreviewMouseRightButtonDown;

            // img
            TheMainWindow.MainImage.PreviewMouseLeftButtonDown += DragAndDrop.DragToExplorer.DragFile;
            TheMainWindow.MainImage.MouseLeftButtonDown += MainImage_MouseLeftButtonDown;
            TheMainWindow.MainImage.MouseLeftButtonUp += MainImage_MouseLeftButtonUp;
            TheMainWindow.MainImage.MouseMove += MainImage_MouseMove;
            TheMainWindow.MainImage.MouseWheel += MainImage_MouseWheel;

            // bg
            TheMainWindow.ParentContainer.MouseLeftButtonDown += Bg_MouseLeftButtonDown;
            TheMainWindow.ParentContainer.Drop += Image_Drop;
            TheMainWindow.ParentContainer.DragEnter += Image_DragEnter;
            TheMainWindow.ParentContainer.DragLeave += Image_DragLeave;
            TheMainWindow.ParentContainer.MouseEnter += Interface_MouseEnter;
            TheMainWindow.ParentContainer.MouseMove += Interface_MouseMove;
            TheMainWindow.ParentContainer.MouseLeave += Interface_MouseLeave;

            // TooltipStyle
            GetToolTipMessage.MouseWheel += MainImage_MouseWheel;

            // TitleBar
            TheMainWindow.TitleBar.MouseLeftButtonDown += Move;
            TheMainWindow.TitleBar.MouseLeave += Restore_From_Move;

            // Lower Bar
            TheMainWindow.LowerBar.Drop += Image_Drop;
            TheMainWindow.LowerBar.MouseLeftButtonDown += MoveAlt;

            // This
            TheMainWindow.Closing += Window_Closing;
            TheMainWindow.StateChanged += MainWindow_StateChanged;

            //LocationChanged += MainWindow_LocationChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

#if DEBUG
            Trace.WriteLine("Events loaded");
#endif
        }

        #region Changed Events

        private static void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (TheMainWindow.WindowState)
            {
                case WindowState.Maximized:
                    AutoFitWindow = false;
                    break;

                case WindowState.Normal:
                    break;

                case WindowState.Minimized:
                    break;
            }
        }

        private static void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Update size when screen resulution changes

            MonitorInfo = MonitorSize.GetMonitorSize();
            TryFitImage();
        }

        #endregion Changed Events

        /// <summary>
        /// Save settings when closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Window_Closing(object sender, CancelEventArgs e)
        {
            // Close Extra windows when closing
            if (fakeWindow != null)
            {
                fakeWindow.Close();
            }

            TheMainWindow.Hide(); // Make it feel faster

            if (!Properties.Settings.Default.AutoFitWindow && !Properties.Settings.Default.Fullscreen)
            {
                Properties.Settings.Default.Top = TheMainWindow.Top;
                Properties.Settings.Default.Left = TheMainWindow.Left;
                Properties.Settings.Default.Height = TheMainWindow.Height;
                Properties.Settings.Default.Width = TheMainWindow.Width;
                Properties.Settings.Default.Maximized = TheMainWindow.WindowState == WindowState.Maximized;
            }

            Properties.Settings.Default.Save();
            DeleteTempFiles();
            RecentFiles.WriteToFile();

#if DEBUG
            Trace.Unindent();
            Trace.WriteLine("Goodbye cruel world!");
            Trace.Flush();
#endif
            Environment.Exit(0);
        }
    }
}