using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        public void AddGallery()
        {
            if (grid.Children.Contains(picGallery))
            {
                return;
            }

            picGallery.Width = 250;
            picGallery.Height = SystemParameters.WorkArea.Height;
            picGallery.HorizontalAlignment = HorizontalAlignment.Right;
            picGallery.Visibility = Visibility.Visible;
            picGallery.Opacity = 1;
            picGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            picGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            //picGallery.Scroller.Margin = new Thickness(1, 1, 0, 0);
            picGallery.Container.Orientation = Orientation.Vertical;
            picGallery.x2.Visibility = Visibility.Collapsed;
            picGallery.Scroller.Margin = new Thickness(0);
            picGallery.Background = new SolidColorBrush(Colors.Transparent);
            grid.Children.Add(picGallery);
        }
    }
}