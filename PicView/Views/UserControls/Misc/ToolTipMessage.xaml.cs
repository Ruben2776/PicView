using System.Windows.Controls;

namespace PicView.UILogic.UserControls
{
    /// <summary>
    /// Interaction logic for SexyToolTip.xaml
    /// </summary>
    public partial class ToolTipMessage : UserControl
    {
        public ToolTipMessage()
        {
            InitializeComponent();
        }

        public ToolTipMessage(string message)
        {
            ToolTipUIText.Text = message;
            InitializeComponent();
        }
    }
}