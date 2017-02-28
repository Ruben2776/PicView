using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace PicView.lib.Windows
{
    /// <summary>
    /// Interaction logic for YesNoDialogWindow.xaml
    /// </summary>
    public partial class YesNoDialogWindow : Window
    {
        public string NameForRename { get { return TxtNewName.Text; } }

        public YesNoDialogWindow(string Message)
        {
            InitializeComponent();

            // CloseButton


            RenameLabel.Content = Message;
        }

        #region Eventhandlers

        // Close Button
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Closing -= Window_Closing;
            //e.Cancel = true;
            AnimationHelper.FadeWindow(this, 0, TimeSpan.FromSeconds(.5));
            FocusManager.SetFocusedElement(Application.Current.MainWindow, this);
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        #endregion


    }
}
