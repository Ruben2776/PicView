using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PicView.Windows
{
    /// <summary>
    /// Interaction logic for FakeWindow.xaml
    /// </summary>
    public partial class FakeWindow : Window
    {
        public FakeWindow()
        {
            InitializeComponent();
            Width = SystemParameters.FullPrimaryScreenWidth;
            Height = SystemParameters.WorkArea.Height;
            GotFocus += FakeWindow_GotFocus;
        }

        private void FakeWindow_GotFocus(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Focus();
        }
    }
}
