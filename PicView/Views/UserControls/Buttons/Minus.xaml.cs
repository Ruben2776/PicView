using PicView.Animations;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class Minus : UserControl
    {
        public Minus()
        {
            InitializeComponent();

            PreviewMouseLeftButtonDown += delegate
            {
                MouseOverAnimations.AltInterfacePreviewMouseOver(PolyFill, BorderBrushKey);
            };

            MouseEnter += delegate
            {
                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            TheButton.Click += (_, _) => SystemCommands.MinimizeWindow(UILogic.ConfigureWindows.GetMainWindow);
        }
    }
}