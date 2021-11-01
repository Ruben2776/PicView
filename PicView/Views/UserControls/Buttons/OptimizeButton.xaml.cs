using PicView.Animations;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Interaction logic for OptimizeButton.xaml
    /// </summary>
    public partial class OptimizeButton : UserControl
    {
        public OptimizeButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    ButtonMouseOverAnim(IconBrush1, false, true);
                    ButtonMouseOverAnim(IconBrush2, false, true);
                    ButtonMouseOverAnim(ButtonBrush, false, true);
                    AnimationHelper.MouseEnterBgTexColor(ButtonBrush);
                };

                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush1);
                    ButtonMouseOverAnim(IconBrush2);
                    AnimationHelper.MouseEnterBgTexColor(ButtonBrush);
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(IconBrush1);
                    ButtonMouseLeaveAnim(IconBrush2);
                    AnimationHelper.MouseLeaveBgTexColor(ButtonBrush);
                };

                TheButton.Click += async (_, _) => await ImageHandling.ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
            };
        }
    }
}
