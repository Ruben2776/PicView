using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using static PicView.Copy_Paste;
using static PicView.DeleteFiles;
using static PicView.DragAndDrop;
using static PicView.Fields;
using static PicView.HideInterfaceLogic;
using static PicView.LoadWindows;
using static PicView.MouseOverAnimations;
using static PicView.Navigation;
using static PicView.Open_Save;
using static PicView.Resize_and_Zoom;
using static PicView.Rotate_and_Flip;
using static PicView.Scroll;
using static PicView.Shortcuts;
using static PicView.UC;
using static PicView.Utilities;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class Eventshandling
    {
        /// <summary>
        /// Start adding events
        /// </summary>
        internal static void Go()
        {
            // keyboard and Mouse_Keys Keys
            mainWindow.KeyDown += MainWindow_KeysDown;
            mainWindow.KeyUp += MainWindow_KeysUp;
            mainWindow.MouseDown += MainWindow_MouseDown;

            // MinButton
            mainWindow.MinButton.TheButton.Click += (s, x) => SystemCommands.MinimizeWindow(mainWindow);

            // MaxButton
            mainWindow.FullscreenButton.TheButton.Click += (s, x) => Fullscreen_Restore();

            // CloseButton
            mainWindow.CloseButton.TheButton.Click += (s, x) => SystemCommands.CloseWindow(mainWindow);

            // FileMenuButton
            mainWindow.FileMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.FolderFill);
            mainWindow.FileMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.FolderFill);
            mainWindow.FileMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.FolderFill);
            mainWindow.FileMenuButton.Click += Toggle_open_menu;

            fileMenu.Open.Click += (s, x) => Open();
            fileMenu.FileLocation.Click += (s, x) => Open_In_Explorer();
            fileMenu.Print.Click += (s, x) => Print(Pics[FolderIndex]);
            fileMenu.SaveBorder.Click += (s, x) => SaveFiles();

            fileMenu.OpenBorder.MouseLeftButtonUp += (s, x) => Open();
            fileMenu.FileLocationBorder.MouseLeftButtonUp += (s, x) => Open_In_Explorer();
            fileMenu.PrintBorder.MouseLeftButtonUp += (s, x) => Print(Pics[FolderIndex]);
            fileMenu.Save_File_Location_Border.MouseLeftButtonUp += (s, x) => SaveFiles();

            fileMenu.PasteButton.Click += (s, x) => Paste();
            fileMenu.CopyButton.Click += (s, x) => Copyfile();

            // image_button
            mainWindow.image_button.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.ImagePath1Fill, mainWindow.ImagePath2Fill, mainWindow.ImagePath3Fill);
            mainWindow.image_button.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.ImagePath1Fill, mainWindow.ImagePath2Fill, mainWindow.ImagePath3Fill);
            mainWindow.image_button.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.ImagePath1Fill, mainWindow.ImagePath2Fill, mainWindow.ImagePath3Fill);
            mainWindow.image_button.Click += Toggle_image_menu;

            // imageSettingsMenu Buttons
            imageSettingsMenu.RotateRightButton.Click += (s, x) => Rotate(true);
            imageSettingsMenu.RotateLeftButton.Click += (s, x) => Rotate(false);

            imageSettingsMenu.Contained_Gallery.Click += delegate {
                Close_UserControls();
                ToggleGallery.OpenContainedGallery();
            };
            imageSettingsMenu.Fullscreen_Gallery.Click += delegate {
                Close_UserControls();
                ToggleGallery.OpenFullscreenGallery();
            };
            imageSettingsMenu.SlideshowButton.Click += delegate { SlideShow.StartSlideshow(); };


            // Quick settings menu
            quickSettingsMenu.ToggleScroll.Checked += (s, x) => IsScrollEnabled = true;
            quickSettingsMenu.ToggleScroll.Unchecked += (s, x) => IsScrollEnabled = false;
            quickSettingsMenu.ToggleScroll.Click += Toggle_quick_settings_menu;

            quickSettingsMenu.SetFit.Click += (s, x) => { AutoFit = true; };
            quickSettingsMenu.SetCenter.Click += (s, x) => { AutoFit = false; };
            quickSettingsMenu.SettingsButton.Click += (s, x) => AllSettingsWindow();

            // LeftButton
            mainWindow.LeftButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.LeftArrowFill);
            mainWindow.LeftButton.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.LeftArrowFill);
            mainWindow.LeftButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.LeftArrowFill);
            mainWindow.LeftButton.Click += (s, x) => PicButton(false, false);

            // RightButton
            mainWindow.RightButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.RightArrowFill);
            mainWindow.RightButton.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.RightArrowFill);
            mainWindow.RightButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.RightArrowFill);
            mainWindow.RightButton.Click += (s, x) => PicButton(false, true);

            // SettingsButton
            mainWindow.SettingsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(mainWindow.SettingsButtonFill);
            mainWindow.SettingsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(mainWindow.SettingsButtonFill);
            mainWindow.SettingsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(mainWindow.SettingsButtonFill);
            mainWindow.SettingsButton.Click += Toggle_quick_settings_menu;

            //FunctionButton
            var MagicBrush = mainWindow.TryFindResource("MagicBrush") as SolidColorBrush;
            mainWindow.FunctionMenuButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(MagicBrush);
            mainWindow.FunctionMenuButton.MouseEnter += (s, x) => ButtonMouseOverAnim(MagicBrush);
            mainWindow.FunctionMenuButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(MagicBrush);
            mainWindow.FunctionMenuButton.Click += Toggle_Functions_menu;

            // FlipButton
            imageSettingsMenu.FlipButton.Click += (s, x) => Flip();

            // ClickArrows
            clickArrowLeft.MouseLeftButtonDown += (s, x) => PicButton(true, false);
            clickArrowLeft.MouseEnter += Interface_MouseEnter_Negative;

            clickArrowRight.MouseLeftButtonDown += (s, x) => PicButton(true, true);
            clickArrowRight.MouseEnter += Interface_MouseEnter_Negative;

            // x2
            x2.MouseLeftButtonDown += (x, xx) => SystemCommands.CloseWindow(mainWindow);
            x2.MouseEnter += Interface_MouseEnter_Negative;

            // Minus
            minus.MouseLeftButtonDown += (s, x) => SystemCommands.MinimizeWindow(mainWindow);
            minus.MouseEnter += Interface_MouseEnter_Negative;

            // GalleryShortcut
            galleryShortcut.MouseLeftButtonDown += (s, x) => ToggleGallery.Toggle();
            galleryShortcut.MouseEnter += Interface_MouseEnter_Negative;

            // Bar
            mainWindow.Bar.MouseLeftButtonDown += Move;
            mainWindow.Bar.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
            mainWindow.Bar.Bar.PreviewKeyDown += CustomTextBox_KeyDown;
            mainWindow.Bar.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;

            // img
            mainWindow.img.MouseLeftButtonDown += Zoom_img_MouseLeftButtonDown;
            mainWindow.img.MouseLeftButtonUp += Zoom_img_MouseLeftButtonUp;
            mainWindow.img.MouseMove += Zoom_img_MouseMove;
            mainWindow.img.MouseWheel += Zoom_img_MouseWheel;

            // bg
            mainWindow.bg.MouseLeftButtonDown += Bg_MouseLeftButtonDown;
            mainWindow.bg.Drop += Image_Drop;
            mainWindow.bg.DragEnter += Image_DragEnter;
            mainWindow.bg.DragLeave += Image_DragLeave;
            mainWindow.bg.MouseEnter += Interface_MouseEnter;
            mainWindow.bg.MouseMove += Interface_MouseMove;
            mainWindow.bg.MouseLeave += Interface_MouseLeave;

            // TooltipStyle
            sexyToolTip.MouseWheel += Zoom_img_MouseWheel;

            // TitleBar
            mainWindow.TitleBar.MouseLeftButtonDown += Move;
            mainWindow.TitleBar.MouseLeave += Restore_From_Move;

            // Logobg
            //Logobg.MouseEnter += LogoMouseOver;
            //Logobg.MouseLeave += LogoMouseLeave;
            //Logobg.PreviewMouseLeftButtonDown += LogoMouseButtonDown;

            // Lower Bar
            mainWindow.LowerBar.Drop += Image_Drop;
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
                    AutoFit = false;
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
            TryZoomFit();
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

            if (!Properties.Settings.Default.AutoFit && !Properties.Settings.Default.Fullscreen)
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
