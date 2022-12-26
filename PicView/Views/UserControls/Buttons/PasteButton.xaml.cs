using System.Windows.Controls;
using PicView.Animations;
using PicView.FileHandling;
using PicView.Properties;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class PasteButton : UserControl
    {
        public PasteButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.MouseEnter += (_, _) => ButtonMouseOverAnim(PasteButtonBrush, true);
                TheButton.MouseLeave += (_, _) => ButtonMouseLeaveAnimBgColor(PasteButtonBrush);

                if (!Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                TheButton.Click += (_, _) => Copy_Paste.PasteAsync();
            };
        }
    }
}