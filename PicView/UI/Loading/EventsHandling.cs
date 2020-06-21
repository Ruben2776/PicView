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
using static PicView.UI.DragAndDrop;
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
            mainWindow.KeyDown += async (s,x) => await MainWindow_KeysDownAsync(s, x).ConfigureAwait(false);
            mainWindow.KeyUp += async (s, x) => await MainWindow_KeysUpAsync(s, x).ConfigureAwait(false);
            mainWindow.MouseDown += async (s, x) => await MainWindow_MouseDownAsync(s, x).ConfigureAwait(false);

            // MinButton
            mainWindow.MinButton.TheButton.Click += (s, x) => SystemCommands.MinimizeWindow(mainWindow);

            // MaxButton
            mainWindow.FullscreenButton.TheButton.Click += (s, x) => Fullscreen_Restore();

            // CloseButton
            mainWindow.CloseButton.TheButton.Click += (s, x) => SystemCommands.CloseWindow(mainWindow);

            // FileMenuButton
            mainWindow.FileMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.FolderFill);
            mainWindow.FileMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.FolderFill);
            mainWindow.FileMenuButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgColor(mainWindow.FileMenuBg);
            mainWindow.FileMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.FolderFill);
            mainWindow.FileMenuButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgColor(mainWindow.FileMenuBg);
            mainWindow.FileMenuButton.Click += Toggle_open_menu;

            fileMenu.Open.Click += (s, x) => Open().ConfigureAwait(false);
            fileMenu.FileLocation.Click += (s, x) => Open_In_Explorer();
            fileMenu.Print.Click += (s, x) => Print(Pics[FolderIndex]);
            fileMenu.SaveBorder.Click += async (s, x) => await SaveFiles().ConfigureAwait(false); ;

            fileMenu.OpenBorder.MouseLeftButtonUp += async (s, x) => await Open().ConfigureAwait(false); ;
            fileMenu.FileLocationBorder.MouseLeftButtonUp += (s, x) => Open_In_Explorer();
            fileMenu.PrintBorder.MouseLeftButtonUp += (s, x) => Print(Pics[FolderIndex]);
            fileMenu.Save_File_Location_Border.MouseLeftButtonUp += async delegate
            {
                await SaveFiles().ConfigureAwait(false);
            };
            fileMenu.PasteButton.Click += async delegate
            {
                await Paste().ConfigureAwait(false);
            };
            fileMenu.CopyButton.Click += (s, x) => Copyfile();

            // image_button
            mainWindow.image_button.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.ImagePath1Fill, mainWindow.ImagePath2Fill, mainWindow.ImagePath3Fill);
            mainWindow.image_button.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.ImagePath1Fill, mainWindow.ImagePath2Fill, mainWindow.ImagePath3Fill);
            mainWindow.image_button.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgColor(mainWindow.ImageMenuBg);
            mainWindow.image_button.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.ImagePath1Fill, mainWindow.ImagePath2Fill, mainWindow.ImagePath3Fill);
            mainWindow.image_button.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgColor(mainWindow.ImageMenuBg);
            mainWindow.image_button.Click += Toggle_image_menu;

            // imageSettingsMenu Buttons
            imageSettingsMenu.RotateRightButton.Click += (s, x) => Rotate(true);
            imageSettingsMenu.RotateLeftButton.Click += (s, x) => Rotate(false);

            imageSettingsMenu.Contained_Gallery.Click += async delegate
            {
                Close_UserControls();
                await GalleryToggle.OpenContainedGallery().ConfigureAwait(false);
            };
            imageSettingsMenu.Fullscreen_Gallery.Click += async delegate
            {
                Close_UserControls();
                await GalleryToggle.OpenFullscreenGallery().ConfigureAwait(false);
            };
            imageSettingsMenu.SlideshowButton.Click += delegate { SlideShow.StartSlideshow(); };

            // LeftButton
            mainWindow.LeftButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.LeftArrowFill);
            mainWindow.LeftButton.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.LeftArrowFill);
            mainWindow.LeftButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(mainWindow.LeftButtonBrush);
            mainWindow.LeftButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.LeftArrowFill);
            mainWindow.LeftButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(mainWindow.LeftButtonBrush);
            mainWindow.LeftButton.Click += async (s, x) => await PicButton(false, false).ConfigureAwait(false);

            // RightButton
            mainWindow.RightButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.RightArrowFill);
            mainWindow.RightButton.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.RightArrowFill);
            mainWindow.RightButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(mainWindow.RightButtonBrush);
            mainWindow.RightButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.RightArrowFill);
            mainWindow.RightButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(mainWindow.RightButtonBrush);
            mainWindow.RightButton.Click += async (s, x) => await PicButton(false, true).ConfigureAwait(false);

            // SettingsButton
            mainWindow.SettingsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.SettingsButtonFill);
            mainWindow.SettingsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.SettingsButtonFill);
            mainWindow.SettingsButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgColor(mainWindow.SettingsMenuBg);
            mainWindow.SettingsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.SettingsButtonFill);
            mainWindow.SettingsButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgColor(mainWindow.SettingsMenuBg);
            mainWindow.SettingsButton.Click += Toggle_quick_settings_menu;

            //FunctionButton
            var MagicBrush = mainWindow.TryFindResource("MagicBrush") as SolidColorBrush;
            mainWindow.FunctionMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(MagicBrush);
            mainWindow.FunctionMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(MagicBrush);
            mainWindow.FunctionMenuButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgColor(mainWindow.EffectsMenuBg);
            mainWindow.FunctionMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(MagicBrush);
            mainWindow.FunctionMenuButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgColor(mainWindow.EffectsMenuBg);
            mainWindow.FunctionMenuButton.Click += Toggle_Functions_menu;

            // FlipButton
            imageSettingsMenu.FlipButton.Click += (s, x) => Flip();

            // ClickArrows
            clickArrowLeft.MouseLeftButtonDown += async (s, x) => await PicButton(true, false).ConfigureAwait(false);
            clickArrowLeft.MouseEnter += Interface_MouseEnter_Negative;

            clickArrowRight.MouseLeftButtonDown += async (s, x) => await PicButton(true, true).ConfigureAwait(false);
            clickArrowRight.MouseEnter += Interface_MouseEnter_Negative;

            // x2
            x2.MouseLeftButtonDown += (x, xx) => SystemCommands.CloseWindow(mainWindow);
            x2.MouseEnter += Interface_MouseEnter_Negative;

            // Minus
            minus.MouseLeftButtonDown += (s, x) => SystemCommands.MinimizeWindow(mainWindow);
            minus.MouseEnter += Interface_MouseEnter_Negative;

            // GalleryShortcut
            galleryShortcut.MouseLeftButtonDown += async (s, x) => await GalleryToggle.ToggleAsync().ConfigureAwait(false);
            galleryShortcut.MouseEnter += Interface_MouseEnter_Negative;

            // Bar
            mainWindow.Bar.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
            mainWindow.Bar.Bar.PreviewKeyDown += async (s, x) => await CustomTextBoxShortcuts.CustomTextBox_KeyDownAsync(s, x).ConfigureAwait(false);
            mainWindow.Bar.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;

            // img
            mainWindow.img.PreviewMouseLeftButtonDown += DragFile;
            mainWindow.img.MouseLeftButtonDown += Zoom_img_MouseLeftButtonDown;
            mainWindow.img.MouseLeftButtonUp += Zoom_img_MouseLeftButtonUp;
            mainWindow.img.MouseMove += Zoom_img_MouseMove;
            mainWindow.img.MouseWheel += async (s, x) => await Zoom_img_MouseWheelAsync(s, x).ConfigureAwait(false);

            // bg
            mainWindow.bg.MouseLeftButtonDown += Bg_MouseLeftButtonDown;
            mainWindow.bg.Drop += async (s, x) => await Image_Drop(s, x).ConfigureAwait(false);
            mainWindow.bg.DragEnter += Image_DragEnter;
            mainWindow.bg.DragLeave += Image_DragLeave;
            mainWindow.bg.MouseEnter += Interface_MouseEnter;
            mainWindow.bg.MouseMove += async (s, x) => await Interface_MouseMoveAsync(s, x).ConfigureAwait(false);
            mainWindow.bg.MouseLeave += async (s, x) => await Interface_MouseLeaveAsync(s, x).ConfigureAwait(false);

            // TooltipStyle
            toolTipMessage.MouseWheel += async (s, x) => await Zoom_img_MouseWheelAsync(s, x).ConfigureAwait(false);

            // TitleBar
            mainWindow.TitleBar.MouseLeftButtonDown += Move;
            mainWindow.TitleBar.MouseLeave += Restore_From_Move;

            // Logobg
            //Logobg.MouseEnter += LogoMouseOver;
            //Logobg.MouseLeave += LogoMouseLeave;
            //Logobg.PreviewMouseLeftButtonDown += LogoMouseButtonDown;

            // Lower Bar
            mainWindow.LowerBar.Drop += async (s, x) => await Image_Drop(s, x).ConfigureAwait(false);
            mainWindow.LowerBar.MouseLeftButtonDown += Move;

            // This
            mainWindow.Closing += Window_Closing;
            mainWindow.StateChanged += MainWindow_StateChanged;
            mainWindow.MouseLeftButtonDown += MoveAlt;

            //LocationChanged += MainWindow_LocationChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

#if DEBUG
            Trace.WriteLine("Events loaded");
#endif
        }

        #region Changed Events

        private static void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (mainWindow.WindowState)
            {
                case WindowState.Maximized:
                    SetWindowBehaviour = false;
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

            mainWindow.Hide(); // Make it feel faster

            if (!Properties.Settings.Default.AutoFitWindow && !Properties.Settings.Default.Fullscreen)
            {
                Properties.Settings.Default.Top = mainWindow.Top;
                Properties.Settings.Default.Left = mainWindow.Left;
                Properties.Settings.Default.Height = mainWindow.Height;
                Properties.Settings.Default.Width = mainWindow.Width;
                Properties.Settings.Default.Maximized = mainWindow.WindowState == WindowState.Maximized;
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