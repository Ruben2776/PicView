using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    public partial class LinkTextBox : UserControl
    {
        public LinkTextBox()
        {
            InitializeComponent();
            linkButton.TheButton.Click += (_, _) => ProcessHandling.ProcessLogic.Hyperlink_RequestNavigate(ValueBox.Text.ToString());
        }

        public LinkTextBox(string url, string name)
        {
            InitializeComponent();
            SetURL(url, name);
            linkButton.TheButton.Click += (_, _) => ProcessHandling.ProcessLogic.Hyperlink_RequestNavigate(ValueBox.Text.ToString());
        }

        public void SetURL(string url, string name)
        {
            ValueBox.Text = url;
            linkButton.ToolTip = url;
            ValueName.Text = name;
        }
    }
}
