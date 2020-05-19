using System.Windows.Controls;
using System.Windows.Input;
using static PicView.Fields;

namespace PicView.UserControls
{
    /// <summary>
    /// Menu to open functions
    /// </summary>
    public partial class FunctionsMenu : UserControl
    {
        public FunctionsMenu()
        {
            InitializeComponent();

            //CloseButton
            CloseButton.MouseEnter += CloseButtonMouseOver;
            CloseButton.MouseLeave += CloseButtonMouseLeave;
            CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;
            CloseButton.Click += delegate { ToggleMenus.Close_UserControls(); };

            // FileDetailsButton
            FileDetailsButton.MouseEnter += FileDetailsButtonMouseOver;
            FileDetailsButton.MouseLeave += FileDetailsButtonMouseLeave;
            FileDetailsButton.PreviewMouseLeftButtonDown += FileDetailsButtonMouseButtonDown;

            //HelpButton
            Info.MouseEnter += HelpButtonMouseOver;
            Info.MouseLeave += HelpButtonMouseLeave;
            Info.PreviewMouseLeftButtonDown += HelpButtonMouseButtonDown;
            Info.Click += delegate { LoadWindows.HelpWindow(); };

            // ClearButton
            ClearButton.MouseEnter += ClearButtonMouseOver;
            ClearButton.MouseLeave += ClearButtonMouseLeave;
            ClearButton.PreviewMouseLeftButtonDown += ClearButtonMouseButtonDown;

            // DeleteButton
            DeleteButton.MouseEnter += DeleteButtonMouseOver;
            DeleteButton.MouseLeave += DeleteButtonMouseLeave;
            DeleteButton.PreviewMouseLeftButtonDown += DeleteButtonMouseButtonDown;

            //DeletePermButton
            DeletePermButton.MouseEnter += DeletePermButtonMouseOver;
            DeletePermButton.MouseLeave += DeleteButtonMousePermLeave;
            DeletePermButton.PreviewMouseLeftButtonDown += DeleteButtonMousePermButtonDown;

            // ReloadButton
            ReloadButton.MouseEnter += ReloadButtonMouseOver;
            ReloadButton.MouseLeave += ReloadButtonMouseLeave;
            ReloadButton.PreviewMouseLeftButtonDown += ReloadButtonMouseButtonDown;

            // ResetZoomButton
            ResetZoomButton.MouseEnter += ResetZoomButtonMouseOver;
            ResetZoomButton.MouseLeave += ResetZoomButtonMouseLeave;
            ResetZoomButton.PreviewMouseLeftButtonDown += ResetZoomButtonMouseButtonDown;

            //SlideshowButton
            SlideshowButton.MouseEnter += SlideshowButtonMouseOver;
            SlideshowButton.MouseLeave += SlideshowButtonMouseLeave;
            SlideshowButton.PreviewMouseLeftButtonDown += SlideshowButtonMouseButtonDown;

            //SlideshowButton
            BgButton.MouseEnter += BgButtonMouseOver;
            BgButton.MouseLeave += BgButtonMouseLeave;
            BgButton.PreviewMouseLeftButtonDown += BgButtonMouseButtonDown;

        }

        #region Mouseover Events

        // Close Button
        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            MouseOverAnimations.ButtonMouseOverAnim(CloseButtonBrush, true);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseOverAnimations.PreviewMouseButtonDownAnim(CloseButtonBrush);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            MouseOverAnimations.ButtonMouseLeaveAnimBgColor(CloseButtonBrush);
        }

        //Help Button
        private void HelpButtonMouseOver(object sender, MouseEventArgs e)
        {
            MouseOverAnimations.ButtonMouseOverAnim(InfoBrush, true);
        }

        private void HelpButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseOverAnimations.PreviewMouseButtonDownAnim(InfoBrush);
        }

        private void HelpButtonMouseLeave(object sender, MouseEventArgs e)
        {
            MouseOverAnimations.ButtonMouseLeaveAnimBgColor(InfoBrush);
        }

        // Reload Button
        private void ReloadButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ReloadBrush,
                false
            );
        }

        private void ReloadButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ReloadBrush, false);
        }

        private void ReloadButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ReloadBrush,
                false
            );
        }

        // ResetZoom Button
        private void ResetZoomButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ResetZoomBrush,
                false
            );
        }

        private void ResetZoomButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ResetZoomBrush, false);
        }

        private void ResetZoomButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ResetZoomBrush,
                false
            );
        }

        // Clear Button
        private void ClearButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ClearBrush,
                false
            );
        }

        private void ClearButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ClearBrush, false);
        }

        private void ClearButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ClearBrush,
                false
            );
        }

        // Delete Button
        private void DeleteButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                DeleteBrush,
                false
            );
        }

        private void DeleteButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(DeleteBrush, false);
        }

        private void DeleteButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                DeleteBrush,
                false
            );
        }


        // Delete Permanent Button
        private void DeletePermButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                DeletePermBrush,
                false
            );
        }

        private void DeleteButtonMousePermButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(DeletePermBrush, false);
        }

        private void DeleteButtonMousePermLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                DeletePermBrush,
                false
            );
        }

        //ToolsWindow Button

        // File Details Button
        private void FileDetailsButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                FileDetailsBrush,
                false
            );
        }

        private void FileDetailsButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FileDetailsBrush, false);
        }

        private void FileDetailsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                FileDetailsBrush,
                false
            );
        }

        //Slideshow Button
        private void SlideshowButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                SlideshowsBrush,
                false
            );
        }

        private void SlideshowButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SlideshowsBrush, false);
        }

        private void SlideshowButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                SlideshowsBrush,
                false
            );
        }

        //Bg Button
        private void BgButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                BgBrush,
                false
            );
        }

        private void BgButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(BgBrush, false);
        }

        private void BgButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                BgBrush,
                false
            );
        }
        #endregion
    }
}
