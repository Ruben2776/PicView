using PicView.FileHandling;
using PicView.UILogic.Animations;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class PasteButton : UserControl
    {
        public PasteButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(PasteButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(PasteButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(PasteButtonBrush, false);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                TheButton.Click += delegate { Copy_Paste.Paste(); };
            };
        }
    }
}