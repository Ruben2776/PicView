using PicView.lib;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PicView.Windows
{
    /// <summary>
    /// A userControl used to inform about the application
    /// </summary>
    public partial class Help : Window
    {
        #region Window Logic

        public Help()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            #region Add Events

            CloseButton.Click += (s, x) => Close();

            CloseButton.MouseEnter += (s, x) =>
            {
                AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CloseButtonBrush, true);
            };

            CloseButton.MouseLeave += (s, x) =>
            {
                AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CloseButtonBrush, true);
            };

            CloseButton.PreviewMouseLeftButtonDown += (s, x) =>
            {
                AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, true);
            };

            KeyDown += Keys;

            #endregion
        }
        
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            AnimationHelper.FadeWindow(this, 0, TimeSpan.FromSeconds(.5));
        }

        #endregion

        #region Keyboard Shortcuts

        private void Keys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape ||
                e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || e.Key == Key.F1)
            {
                Close();
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
    }
}
