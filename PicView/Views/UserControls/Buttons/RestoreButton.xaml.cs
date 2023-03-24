using PicView.Animations;
using PicView.Properties;
using PicView.UILogic.Sizing;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls.Buttons
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class Restorebutton : UserControl
    {
        public Restorebutton()
        {
            InitializeComponent();

            TheButton.Click += delegate { WindowSizing.Fullscreen_Restore(!Settings.Default.Fullscreen); };

            MouseEnter += delegate
            {
                if (!Settings.Default.Fullscreen)
                {
                    ToolTip = Application.Current.Resources["Fullscreen"];
                }
                else
                {
                    ToolTip = Application.Current.Resources["RestoreDown"];
                }

                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
            };
        }
    }
}