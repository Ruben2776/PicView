using Nepharia.lib;
using PicView.lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Nepharia.UserControls
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
