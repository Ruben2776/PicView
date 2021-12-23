using PicView.Animations;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class Restorebutton : UserControl
    {
        public Restorebutton()
        {
            InitializeComponent();

            TheButton.Click += delegate { UILogic.Sizing.WindowSizing.Fullscreen_Restore(); };

            MouseEnter += delegate
            {
                if (!Properties.Settings.Default.Fullscreen)
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