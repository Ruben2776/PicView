using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class CopyButton : UserControl
    {
        public CopyButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                SetButtonIconMouseOverAnimations(TheButton, ButtonBrush, IconBrush);
            };
        }
    }
}