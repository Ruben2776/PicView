using PicView.PicGallery;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using static PicView.SystemIntegration.NativeMethods;

namespace PicView.Views.Windows
{
    public partial class FakeWindow : Window
    {
        public FakeWindow()
        {
            Focusable = false;
            Topmost = false;
            InitializeComponent();
            Width = WindowSizing.MonitorInfo.Width;
            Height = WindowSizing.MonitorInfo.Height;
            Width = WindowSizing.MonitorInfo.Width;
            Height = WindowSizing.MonitorInfo.Height;
            ContentRendered += FakeWindow_ContentRendered;
        }

        private void FakeWindow_ContentRendered(object sender, EventArgs e)
        {
            PreviewMouseLeftButtonDown += FakeWindow_MouseLeftButtonDown;
            PreviewMouseRightButtonDown += FakeWindow_MouseLeftButtonDown;
            Application.Current.MainWindow.StateChanged += MainWindow_StateChanged;
            StateChanged += FakeWindow_StateChanged;
            ConfigureWindows.GetMainWindow.Focus();
            GotFocus += FakeWindow_LostFocus;

            EnableBlur(this);

            // Hide from alt tab
            var helper = new WindowInteropHelper(this).Handle;
            _ = SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

            ConfigureWindows.GetMainWindow.BringIntoView();
        }

        private void FakeWindow_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigureWindows.GetMainWindow.Focus();
        }

        private void FakeWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                ConfigureWindows.GetMainWindow.Focus();
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.WindowState == WindowState.Normal && GalleryFunctions.IsVerticalFullscreenOpen 
                || ConfigureWindows.GetMainWindow.WindowState == WindowState.Normal && GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                if (IsVisible is false)
                {
                    Show();
                    Activate();
                }

                ConfigureWindows.GetMainWindow.BringIntoView();
                ConfigureWindows.GetMainWindow.Activate();
            }
            else if (ConfigureWindows.GetMainWindow.WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void FakeWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ConfigureWindows.GetMainWindow.Focus();
        }

        private bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }
    }
}