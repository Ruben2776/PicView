using PicView.Animations;
using PicView.UILogic.TransformImage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                TheButton.Checked += delegate
                {
                    FlipPath.Data = Geometry.Parse("M448,192l-128,96v-64H128v128h248c4.4,0,8,3.6,8,8v48c0,4.4-3.6,8-8,8H72c-4.4,0-8-3.6-8-8V168c0-4.4,3.6-8,8-8h248V96 L448, 192z");
                };

                TheButton.Unchecked += delegate
                {
                    FlipPath.Data = Geometry.Parse("M192,96v64h248c4.4,0,8,3.6,8,8v240c0,4.4-3.6,8-8,8H136c-4.4,0-8-3.6-8-8v-48c0-4.4,3.6-8,8-8h248V224H192v64L64,192 L192, 96z");
                };
            };
        }
    }
}