using System.Windows;
using PicView.WPF.Animations;
using PicView.WPF.UILogic;

namespace PicView.WPF.Views.UserControls.Buttons
{
    public partial class Minus
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
            Loaded += delegate
            {
                ToolTip = Core.Localization.TranslationHelper.GetTranslation("Minimize");
            };
        }
    }
}