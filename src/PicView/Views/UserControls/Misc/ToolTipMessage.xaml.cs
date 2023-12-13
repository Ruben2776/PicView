using PicView.WPF.Shortcuts;

namespace PicView.WPF.Views.UserControls.Misc
{
    public partial class ToolTipMessage
    {
        public ToolTipMessage()
        {
            InitializeComponent();
            MouseWheel += async (sender, e) =>
                await MainMouseKeys.MainImage_MouseWheelAsync(sender, e).ConfigureAwait(false);
        }
    }
}