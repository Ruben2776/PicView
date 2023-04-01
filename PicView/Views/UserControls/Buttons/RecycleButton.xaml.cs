using PicView.Animations;
using PicView.FileHandling;
using PicView.Properties;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class RecycleButton : UserControl
    {
        public RecycleButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                SetIconButterMouseOverAnimations(TheButton, ButtonBrush, IconBrush);

                TheButton.Click += async (_, _) => await DeleteFiles.DeleteFileAsync(false).ConfigureAwait(false);
            };
        }
    }
}