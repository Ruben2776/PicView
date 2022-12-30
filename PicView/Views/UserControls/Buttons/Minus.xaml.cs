using PicView.Animations;
using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls.Buttons
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class Minus : UserControl
    {
        public Minus()
        {
            InitializeComponent();

            MouseEnter += delegate
            {
                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            TheButton.Click += (_, _) => SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow);
        }
    }
}