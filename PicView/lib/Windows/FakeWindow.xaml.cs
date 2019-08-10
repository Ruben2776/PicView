using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.lib.Helper;
using static PicView.lib.Variables;

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
            Width = MonitorInfo.Width;
            Height = MonitorInfo.Height;
            Width = MonitorInfo.Width;
            Height = MonitorInfo.Height;
            ContentRendered += FakeWindow_ContentRendered;

        }

        private void FakeWindow_ContentRendered(object sender, EventArgs e)
        {
            MouseLeftButtonDown += FakeWindow_MouseLeftButtonDown;
            MouseRightButtonDown += FakeWindow_MouseLeftButtonDown;
            Application.Current.MainWindow.StateChanged += MainWindow_StateChanged;
            StateChanged += FakeWindow_StateChanged;
            MouseWheel += FakeWindow_MouseWheel;
            Application.Current.MainWindow.Focus();
            LostFocus += FakeWindow_LostFocus;
        }

        private void FakeWindow_LostFocus(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Focus();
        }

        private void FakeWindow_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    Application.Current.MainWindow.Focus();
                    break;
                case WindowState.Minimized:
                    break;
                case WindowState.Maximized:
                    break;
                default:
                    break;
            }
        }

        private void FakeWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                picGallery.ScrollTo(e.Delta > 0, true);
            else
                picGallery.ScrollTo(e.Delta > 0, false, true);
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (Application.Current.MainWindow.WindowState)
            {
                case WindowState.Normal:
                    Show();
                    Application.Current.MainWindow.Focus();
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