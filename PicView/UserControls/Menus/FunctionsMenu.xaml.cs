using System.Windows.Controls;
using System.Windows.Input;

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

        
        #endregion
    }
}
