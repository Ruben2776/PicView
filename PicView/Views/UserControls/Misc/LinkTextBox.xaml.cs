using PicView.ProcessHandling;
using System.Windows.Controls;

namespace PicView.Views.UserControls.Misc
{
    public partial class LinkTextBox : UserControl
    {
        public LinkTextBox()
        {
            InitializeComponent();
            linkButton.TheButton.Click += (_, _) => ProcessLogic.Hyperlink_RequestNavigate(ValueBox.Text);
        }

        public LinkTextBox(string url, string name)
        {
            InitializeComponent();
            SetURL(url, name);
            linkButton.TheButton.Click += (_, _) => ProcessLogic.Hyperlink_RequestNavigate(ValueBox.Text);
        }

        public void SetURL(string url, string name)
        {
            ValueBox.Text = url;
            linkButton.ToolTip = url;
            ValueName.Text = name;
        }
    }
}