using System.Windows.Controls;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// Interaction logic for SexyToolTip.xaml
    /// </summary>
    public partial class SexyToolTip : UserControl
    {

        public SexyToolTip()
        {
            InitializeComponent();
        }

        public SexyToolTip(string message)
        {
            SexyToolTipText.Text = message;
            InitializeComponent();
        }
    }
}
