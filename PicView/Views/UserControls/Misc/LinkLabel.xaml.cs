using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LinkLabel.xaml
    /// </summary>
    public partial class LinkLabel : UserControl
    {
        public LinkLabel()
        {
            InitializeComponent();
            txt.Click += (_, _) => ProcessHandling.ProcessLogic.Hyperlink_RequestNavigate(txt.Content.ToString());
        }

        public LinkLabel(string url)
        {
            InitializeComponent();
            SetURL(url);
            txt.Click += (_, _) => ProcessHandling.ProcessLogic.Hyperlink_RequestNavigate(txt.Content.ToString());
        }

        public void SetURL(string url)
        {
            txt.Content = url;
        }
    }
}
