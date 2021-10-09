using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Interaction logic for SexyToolTip.xaml
    /// </summary>
    public partial class ToolTipMessage : UserControl
    {
        public ToolTipMessage()
        {
            InitializeComponent();
            MouseWheel += async (sender, e) => await Shortcuts.MainShortcuts.MainImage_MouseWheelAsync(sender, e).ConfigureAwait(false);
        }

        public ToolTipMessage(string message)
        {
            ToolTipUIText.Text = message;
            InitializeComponent();
        }
    }
}