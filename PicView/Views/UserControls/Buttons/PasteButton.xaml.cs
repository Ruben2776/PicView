using PicView.FileHandling;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons;

public partial class PasteButton : UserControl
{
    public PasteButton()
    {
        InitializeComponent();

        Loaded += delegate
        {
            SetButtonIconMouseOverAnimations(TheButton, ButtonBrush, IconBrush);

            TheButton.Click += async delegate { await CopyPaste.PasteAsync().ConfigureAwait(false); };
        };
    }
}