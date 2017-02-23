using System.Windows.Controls;
using System.Windows.Input;
using static PicView.lib.Variables;

namespace PicView.lib.UserControls.Menus
{
    /// <summary>
    /// Interaction logic for EkstraMenu.xaml
    /// </summary>
    public partial class EkstraMenu : UserControl
    {
        public EkstraMenu()
        {
            InitializeComponent();


            #region Register Events

            //CloseButton
            CloseButton.MouseEnter += CloseButtonMouseOver;
            CloseButton.MouseLeave += CloseButtonMouseLeave;
            CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;

            // SettingsButton
            SettingsButton.MouseEnter += SettingsButtonMouseOver;
            SettingsButton.MouseLeave += SettingsButtonMouseLeave;
            SettingsButton.PreviewMouseLeftButtonDown += SettingsButtonMouseButtonDown;

            //HelpButton
            Help.MouseEnter += HelpButtonMouseOver;
            Help.MouseLeave += HelpButtonMouseLeave;
            Help.PreviewMouseLeftButtonDown += HelpButtonMouseButtonDown;

            //AboutButton
            About.MouseEnter += AboutButtonMouseOver;
            About.MouseLeave += AboutButtonMouseLeave;
            About.PreviewMouseLeftButtonDown += AboutButtonMouseButtonDown;

            #endregion


        }

        #region Mouseover Events

        // Close Button
        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
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
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, SettingsButtonBrush, false);
        }

        private void SettingsButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SettingsButtonBrush, false);
        }

        private void SettingsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, SettingsButtonBrush, false);
        }

        //Help Button
        private void HelpButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                HelpBorderBrush,
                false
            );
        }

        private void HelpButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(HelpBorderBrush, false);
        }

        private void HelpButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                HelpBorderBrush,
                false
            );
        }

        //About Button

        private void AboutButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                AboutBorderBrush,
                false
            );
        }

        private void AboutButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(AboutBorderBrush, false);
        }

        private void AboutButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                AboutBorderBrush,
                false
            );
        }
        #endregion
    }
}
