using PicView.Animations;
using PicView.FileHandling;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class PasteButton : UserControl
    {
        public PasteButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (_, _) => PreviewMouseButtonDownAnim(PasteButtonBrush);
                TheButton.MouseEnter += (_, _) => ButtonMouseOverAnim(PasteButtonBrush, true);
                TheButton.MouseLeave += (_, _) => ButtonMouseLeaveAnimBgColor(PasteButtonBrush, false);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                TheButton.Click += (_, _) => Copy_Paste.Paste();
            };
        }
    }
}