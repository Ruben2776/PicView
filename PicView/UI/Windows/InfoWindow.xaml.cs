using PicView.UI.Animations;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using static PicView.Library.Fields;

namespace PicView.UI.Windows
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
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            KeyDown += KeysDown;
            KeyUp += KeysUp;
            Scroller.MouseWheel += Info_MouseWheel;

            // CloseButton
            CloseButton.TheButton.Click += delegate { HideLogic(); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            Aller.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(AllerBrush); };
            Aller.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(AllerBrush); };
            Aller.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(AllerBrush); };

            TexGyre.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(TexGyreBrush); };
            TexGyre.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(TexGyreBrush); };
            TexGyre.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(TexGyreBrush); };

            Iconic.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(IconicBrush); };
            Iconic.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(IconicBrush); };
            Iconic.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(IconicBrush); };

            FlatIcon.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(FlatIconBrush); };
            FlatIcon.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(FlatIconBrush); };
            FlatIcon.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(FlatIconBrush); };

            Ionic.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(IonicBrush); };
            Ionic.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(IonicBrush); };
            Ionic.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(IonicBrush); };

            FontAwesome.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(FontAwesomeBrush); };
            FontAwesome.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(FontAwesomeBrush); };
            FontAwesome.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(FontAwesomeBrush); };

            GitHub.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(GitHubBrush); };
            GitHub.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(GitHubBrush); };
            GitHub.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(GitHubBrush); };

            zondicons.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(zondiconsBrush); };
            zondicons.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(zondiconsBrush); };
            zondicons.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(zondiconsBrush); };
        }

        #region Keyboard Shortcuts

        private void KeysDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                case Key.PageDown:
                case Key.S:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + zoomSpeed);
                    break;

                case Key.Up:
                case Key.PageUp:
                case Key.W:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - zoomSpeed);
                    break;

                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Environment.Exit(0);
                    }
                    break;
            }
        }

        private void KeysUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    HideLogic();
                    break;

                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Environment.Exit(0);
                    }
                    break;
            }
        }

        private void Info_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - zoomSpeed);
            }
            else if (e.Delta < 0)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + zoomSpeed);
            }
        }

        #endregion Keyboard Shortcuts

        private void HideLogic()
        {
            var da = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(.3)
            };

            da.Completed += delegate
            {
                Hide();
            };

            TheMainWindow.Effect = null;
            BeginAnimation(OpacityProperty, da);
            TheMainWindow.Focus();
        }

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