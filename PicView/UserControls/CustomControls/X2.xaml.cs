using PicView.Helpers;
using System.Windows.Controls;

namespace PicView.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class X2 : UserControl
    {
        public X2()
        {
            InitializeComponent();

            MouseEnter += (sender, e) =>
            {
                AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, CrossFill, false);
            };
            MouseLeave += (sender, e) =>
            {
                AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, CrossFill, false);
            };
        }
    }
}
