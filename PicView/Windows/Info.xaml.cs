using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using static PicView.Fields;

namespace PicView.Windows
{
    public partial class Info : Window
    {
        #region Window Logic

        public Info()
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
            #region Add events

            KeyDown += Keys;

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); mainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            #endregion
        }

        #endregion

        #region Keyboard Shortcuts

        private void Keys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape ||
                e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || e.Key == Key.F1)
            {
                Hide();
            }

            else if (e.Key == Key.S || e.Key == Key.Down)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 10);
            }

            else if (e.Key == Key.W || e.Key == Key.Up)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 10);
            }
        }

        #endregion

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
