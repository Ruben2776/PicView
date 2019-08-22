using System;
using System.Windows;
using System.Windows.Input;
using static PicView.Fields;

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
            mainWindow.Focus();
        }

        private void FakeWindow_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    mainWindow.Focus();
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
                PicGalleryLogic.ScrollTo(e.Delta > 0, true);
            else
                PicGalleryLogic.ScrollTo(e.Delta > 0, false, true);
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (mainWindow.WindowState)
            {
                case WindowState.Normal:
                    Show();
                    mainWindow.Focus();
                    break;

                case WindowState.Minimized:
                    Hide();
                    break;
            }
        }

        private void FakeWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.Focus();
        }
    }
}