using PicView.UILogic.Sizing;
using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.UILogic.UserControls
{
    public partial class FullscreenButton : UserControl
    {
        public FullscreenButton()
        {
            InitializeComponent();

            PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FullscreenButtonBrush); };
            MouseEnter += delegate 
            {
                if (Properties.Settings.Default.Fullscreen)
                {
                    ToolTip = Application.Current.Resources["RestoreDown"];
                }
                else
                {
                    ToolTip = Application.Current.Resources["Fullscreen"];
                }

                ButtonMouseOverAnim(FullscreenButtonBrush, true);
            };

            MouseLeave += delegate 
            {
                ButtonMouseLeaveAnim(FullscreenButtonBrush, true);
            };;

            TheButton.Click += delegate 
            {
                WindowLogic.Fullscreen_Restore();
            }; 

        }
    }
}