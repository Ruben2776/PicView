using PicView.UILogic.Animations;
using PicView.UILogic.Sizing;
using PicView.UILogic.TransformImage;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PicView.Views.Windows
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();

            // Get version
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            appVersion.Content += fvi.FileVersion;

            ContentRendered += Window_ContentRendered;

            if (Properties.Settings.Default.LightTheme)
            {
                var Source = new BitmapImage();
                Source.BeginInit();
                Source.UriSource = new Uri("pack://application:,,,/PicView;component/Themes/Resources/img/icon-raster.128b.png");
                Source.EndInit();

                Logo.Source = Source;
            }
            MaxWidth = MinWidth = 565 * WindowLogic.MonitorInfo.DpiScaling;
            Top = 10 * WindowLogic.MonitorInfo.DpiScaling;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            KeyDown += KeysDown;
            Scroller.MouseWheel += Info_MouseWheel;
            Bar.MouseLeftButtonDown += (_, _) => DragMove();
            Closing += (_, e) =>
            {
                Hide();
                e.Cancel = true;
            };

            CloseButton.TheButton.Click += delegate { Close(); };
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            Iconic.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(IconicBrush); };
            Iconic.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(IconicBrush); };
            Iconic.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(IconicBrush); };

            Ionic.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(IonicBrush); };
            Ionic.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(IonicBrush); };
            Ionic.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(IonicBrush); };

            FontAwesome.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(FontAwesomeBrush); };
            FontAwesome.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(FontAwesomeBrush); };
            FontAwesome.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(FontAwesomeBrush); };

            GitHub.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(GitHubBrush); };
            GitHub.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(GitHubBrush); };
            GitHub.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(GitHubBrush); };

            License.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(LicenseBrush); };
            License.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(LicenseBrush); };
            License.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(LicenseBrush); };

            zondicons.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(zondiconsBrush); };
            zondicons.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(zondiconsBrush); };
            zondicons.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(zondiconsBrush); };            
        }

        #region Keyboard Shortcuts

        private void KeysDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                case Key.F1:
                    Close();
                    break;

                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Environment.Exit(0);
                    }
                    break;

                case Key.Down:
                case Key.PageDown:
                case Key.S:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + ZoomLogic.zoomSpeed);
                    break;

                case Key.Up:
                case Key.PageUp:
                case Key.W:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - ZoomLogic.zoomSpeed);
                    break;
            }
        }

        private void Info_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - ZoomLogic.zoomSpeed);
            }
            else if (e.Delta < 0)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + ZoomLogic.zoomSpeed);
            }
        }

        #endregion Keyboard Shortcuts

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var ps = new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }
    }
}