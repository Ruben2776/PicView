using PicView.FileHandling;
using PicView.Animations;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class RecycleButton : UserControl
    {
        public RecycleButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RecycleButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RecycleButtonBrush, false);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                TheButton.Click += async (_, _) => await DeleteFiles.DeleteFileAsync(false);
            };
        }
    }
}