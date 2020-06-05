using System.Windows.Controls;
using System.Windows.Input;
using static PicView.Fields;
using static PicView.Wallpaper;

namespace PicView.UserControls
{
    /// <summary>
    /// Interaction logic for QuickSettingsMenu.xaml
    /// </summary>
    public partial class QuickSettingsMenu : UserControl
    {
        public QuickSettingsMenu()
        {
            InitializeComponent();

            #region Register events

            // SettingsButton
            SettingsButton.MouseEnter += SettingsButtonMouseOver;
            SettingsButton.MouseLeave += SettingsButtonMouseLeave;
            SettingsButton.PreviewMouseLeftButtonDown += SettingsButtonMouseButtonDown;
            SettingsButton.Click += delegate {
                UC.Close_UserControls();
                LoadWindows.AllSettingsWindow();
            };

            // InfoButton
            InfoButton.MouseEnter += InfoButtonMouseOver;
            InfoButton.MouseLeave += InfoButtonMouseLeave;
            InfoButton.PreviewMouseLeftButtonDown += InfoButtonMouseButtonDown;
            InfoButton.Click += delegate {
                UC.Close_UserControls();
                LoadWindows.HelpWindow();
            };

            // Zoom button
            ZoomButton.MouseEnter += ZoomButtonMouseOver;
            ZoomButton.MouseLeave += ZoomButtonMouseLeave;
            ZoomButton.PreviewMouseLeftButtonDown += ZoomButtonMouseButtonDown;
            ZoomButton.Click += delegate {
                UC.Close_UserControls();
                Resize_and_Zoom.ResetZoom();
            };

            // Toggle Scroll
            ToggleScroll.PreviewMouseLeftButtonDown += ToggleScroll_PreviewMouseLeftButtonDown;
            ToggleScroll.MouseEnter += ToggleScroll_MouseEnter;
            ToggleScroll.MouseLeave += ToggleScroll_MouseLeave;

            // BgButton
            BgButton.PreviewMouseLeftButtonDown += BgButton_PreviewMouseLeftButtonDown;
            BgButton.MouseEnter += BgButton_MouseEnter;
            BgButton.MouseLeave += BgButton_MouseLeave;
            BgButton.Click += Utilities.ChangeBackground;

            // Set Fit
            SetFit.PreviewMouseLeftButtonDown += SetFit_PreviewMouseLeftButtonDown;
            SetFit.MouseEnter += SetFit_MouseEnter;
            SetFit.MouseLeave += SetFit_MouseLeave;

            // Fill
            Fill.PreviewMouseLeftButtonDown += Fill_PreviewMouseLeftButtonDown;
            Fill.MouseEnter += Fill_MouseEnter;
            Fill.MouseLeave += Fill_MouseLeave;
            Fill.Click += (s, x) => SetWallpaper(Pics[FolderIndex], WallpaperStyle.Fill);

            // Fit
            Fit.PreviewMouseLeftButtonDown += Fit_PreviewMouseLeftButtonDown;
            Fit.MouseEnter += Fit_MouseEnter;
            Fit.MouseLeave += Fit_MouseLeave;
            Fit.Click += (s, x) => SetWallpaper(Pics[FolderIndex], WallpaperStyle.Fit);

            // Center
            Center.PreviewMouseLeftButtonDown += Center_PreviewMouseLeftButtonDown;
            Center.MouseEnter += Center_MouseEnter;
            Center.MouseLeave += Center_MouseLeave;
            Center.Click += (s, x) => SetWallpaper(Pics[FolderIndex], WallpaperStyle.Center);

            // Tile
            Tile.PreviewMouseLeftButtonDown += Tile_PreviewMouseLeftButtonDown;
            Tile.MouseEnter += Tile_MouseEnter;
            Tile.MouseLeave += Tile_MouseLeave;
            Tile.Click += (s, x) => SetWallpaper(Pics[FolderIndex], WallpaperStyle.Tile);

            // Stretch
            Stretch.PreviewMouseLeftButtonDown += Stretch_PreviewMouseLeftButtonDown;
            Stretch.MouseEnter += Stretch_MouseEnter;
            Stretch.MouseLeave += Stretch_MouseLeave;
            Stretch.Click += (s, x) => SetWallpaper(Pics[FolderIndex], WallpaperStyle.Stretch);

            // Set Center
            SetCenter.PreviewMouseLeftButtonDown += SetCenter_PreviewMouseLeftButtonDown;
            SetCenter.MouseEnter += SetCenter_MouseEnter;
            SetCenter.MouseLeave += SetCenter_MouseLeave;



            #endregion
        }

        #region Mouseover Events


        // Settings Button

        private void SettingsButtonMouseOver(object sender, MouseEventArgs e)
        {
            MouseOverAnimations.ButtonMouseOverAnim(SettingsButtonBrush, true);
        }

        private void SettingsButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseOverAnimations.PreviewMouseButtonDownAnim(SettingsButtonBrush);
        }

        private void SettingsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            MouseOverAnimations.ButtonMouseLeaveAnimBgColor(SettingsButtonBrush);
        }

        // Info Button

        private void InfoButtonMouseOver(object sender, MouseEventArgs e)
        {
            MouseOverAnimations.ButtonMouseOverAnim(InfoButtonBrush, true);
        }

        private void InfoButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseOverAnimations.PreviewMouseButtonDownAnim(InfoButtonBrush);
        }

        private void InfoButtonMouseLeave(object sender, MouseEventArgs e)
        {
            MouseOverAnimations.ButtonMouseLeaveAnimBgColor(InfoButtonBrush);
        }

        // Fill Button
        void Fill_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FillBrush,
                false
            );
        }

        void Fill_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FillBrush,
                false
            );
        }

        void Fill_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FillBrush, false);
        }

        // Tile Button
        void Tile_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                TileBrush,
                false
            );
        }

        void Tile_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                TileBrush,
                false
            );
        }

        void Tile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TileBrush, false);
        }

        // Center Button
        void Center_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                CenterBrush,
                false
            );
        }

        void Center_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                CenterBrush,
                false
            );
        }

        void Center_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CenterBrush, false);
        }

        // Fitbutton Mouse Event
        void Fit_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FitBrush,
                false
            );
        }

        void Fit_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FitBrush,
                false
            );
        }

        void Fit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FitBrush, false);
        }

        // Stretch Button
        void Stretch_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                StretchBrush,
                false
            );
        }

        void Stretch_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                StretchBrush,
                false
            );
        }

        void Stretch_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(StretchBrush, false);
        }

        // SetFitbutton Mouse Event
        void SetFit_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SetFitBrush,
                false
            );
        }

        void SetFit_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SetFitBrush,
                false
            );
        }

        void SetFit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SetFitBrush, false);
        }

        // SetCenter Button
        void SetCenter_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SetCenterBrush,
                false
            );
        }

        void SetCenter_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SetCenterBrush,
                false
            );
        }

        void SetCenter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SetCenterBrush, false);
        }

        // BgButton
        void BgButton_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                BgButtonBrush,
                false
            );
        }

        void BgButton_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                BgButtonBrush,
                false
            );
        }

        void BgButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(BgButtonBrush, false);
        }

        // ToggleScroll
        void ToggleScroll_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                ToggleScrollBrush,
                false
            );
        }

        void ToggleScroll_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                ToggleScrollBrush,
                false
            );
        }

        void ToggleScroll_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ToggleScrollBrush, false);
        }


        // Zoom Button
        private void ZoomButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, ZoomButtonBrush, false);
        }

        private void ZoomButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ZoomButtonBrush, false);
        }

        private void ZoomButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                ZoomButtonBrush,
                false
            );
        }

        #endregion

    }
}
