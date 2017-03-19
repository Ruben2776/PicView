using System;
using System.Windows;
using System.Windows.Input;

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
            MouseLeftButtonDown += FakeWindow_MouseLeftButtonDown;
            Application.Current.MainWindow.StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (Application.Current.MainWindow.WindowState)
            {
                case WindowState.Normal:
                    Show();
                    break;
                case WindowState.Minimized:
                    Hide();
                    break;
                case WindowState.Maximized:
                    break;
                default:
                    break;
            }
        }

        private void FakeWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.MainWindow.Focus();
        }
    }
}
