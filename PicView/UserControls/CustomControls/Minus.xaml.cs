using PicView.Helpers;
using System.Windows.Controls;

namespace PicView.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class Minus : UserControl
    {
        public Minus()
        {
            InitializeComponent();

            MouseEnter += (sender, e) =>
            {
                AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, MinusFill, false);
            };
            MouseLeave += (sender, e) =>
            {
                AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, MinusFill, false);
            };
        }
    }
}
