using PicView.lib;
using System.Windows;
using System.Windows.Controls;

namespace PicView.UserControls
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class Help : UserControl
    {
        public Help()
        {
            InitializeComponent();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Helper.Close(this);
        }
    }
}
