using PicView.Animations;
using PicView.ImageHandling;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
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
                var IconBrush = (SolidColorBrush)Resources["IconBrush"];
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    ButtonMouseOverAnim(IconBrush, false, true);
                    ButtonMouseOverAnim(ButtonBrush, false, true);
                    AnimationHelper.MouseEnterBgTexColor(ButtonBrush);
                };

                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush);
                    AnimationHelper.MouseEnterBgTexColor(ButtonBrush);
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(IconBrush);
                    AnimationHelper.MouseLeaveBgTexColor(ButtonBrush);
                };

                TheButton.Click += async (_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
            };
        }
    }
}