using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.UILogic.TransformImage;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class FlipButton : UserControl
    {
        public FlipButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    ButtonMouseOverAnim(IconBrush, false, true);
                    ButtonMouseOverAnim(TheButtonBrush, false, true);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);
                };

                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);

                    if (TheButton.IsChecked.HasValue == false) { return; }
                    ToolTip = TheButton.IsChecked.Value ?
                        Application.Current.Resources["Unflip"] :
                        Application.Current.Resources["Flip"];
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(IconBrush);
                    AnimationHelper.MouseLeaveBgTexColor(TheButtonBrush);
                };

                TheButton.Click += delegate
                {
                    Rotation.Flip();
                };

                // Change FlipButton's icon when (un)checked
                TheButton.Checked += (_, _) => UpdateUIValues.ChangeFlipButton(true, FlipPath);
                TheButton.Unchecked += (_, _) => UpdateUIValues.ChangeFlipButton(false, FlipPath);
            };
        }
    }
}