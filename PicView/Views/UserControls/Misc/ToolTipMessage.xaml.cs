using System.Windows.Controls;
using PicView.Shortcuts;

namespace PicView.Views.UserControls.Misc
{
    public partial class ToolTipMessage : UserControl
    {
        public ToolTipMessage()
        {
            InitializeComponent();
            MouseWheel += async (sender, e) => await MainMouseKeys.MainImage_MouseWheelAsync(sender, e).ConfigureAwait(false);
        }

        public ToolTipMessage(string message)
        {
            ToolTipUIText.Text = message;
            InitializeComponent();
        }
    }
}