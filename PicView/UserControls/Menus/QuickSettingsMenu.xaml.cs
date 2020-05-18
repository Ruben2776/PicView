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

            // CloseButton
            CloseButton.MouseEnter += CloseButtonMouseOver;
            CloseButton.MouseLeave += CloseButtonMouseLeave;
            CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;

            // SettingsButton
            SettingsButton.MouseEnter += SettingsButtonMouseOver;
            SettingsButton.MouseLeave += SettingsButtonMouseLeave;
            SettingsButton.PreviewMouseLeftButtonDown += SettingsButtonMouseButtonDown;


            // Toggle Scroll
            ToggleScroll.PreviewMouseLeftButtonDown += ToggleScroll_PreviewMouseLeftButtonDown;
            ToggleScroll.MouseEnter += ToggleScroll_MouseEnter;
            ToggleScroll.MouseLeave += ToggleScroll_MouseLeave;

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

        // Close Button
        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }


        // Settings Button

        private void SettingsButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(0, 0, 0, 0, SettingsButtonBrush, false);
        }

        private void SettingsButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SettingsButtonBrush, false);
        }

        private void SettingsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, SettingsButtonBrush, false);
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

        #endregion

    }
}
