using System.Windows.Controls;
using PicView.FileHandling;
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
                SetButtonIconMouseOverAnimations(TheButton, ButtonBrush, IconBrush);

                TheButton.Click += async delegate { await Copy_Paste.PasteAsync().ConfigureAwait(false); };
            };
        }
    }
}