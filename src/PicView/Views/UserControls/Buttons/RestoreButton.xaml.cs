using System.Windows;
using PicView.WPF.Animations;
using PicView.WPF.Properties;
using PicView.WPF.UILogic.Sizing;

namespace PicView.WPF.Views.UserControls.Buttons
{
    public partial class RestoreButton
    {
        public RestoreButton()
        {
            InitializeComponent();

            TheButton.Click += delegate { WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen); };

            MouseEnter += delegate
            {
                ToolTip =
                    !Settings.Default.Fullscreen
                        ? Application.Current.Resources["Fullscreen"]
                        : Application.Current.Resources["RestoreDown"];

                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
            };
        }
    }
}