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

        #region Constructor
        public Help()
        {
            InitializeComponent();
        }
        #endregion

        #region Loaded

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            #region Add Events

            #region CloseButton

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

            #endregion

            KeyDown += Keys;

            #endregion
        }

        #endregion

        #region Closing

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            AnimationHelper.FadeWindow(this, 0, TimeSpan.FromSeconds(.5));
        }

        #endregion

        #endregion

        #region Keyboard Shortcuts

        private void Keys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape ||
                e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || e.Key == Key.F1)
            {
                Close();
            }
        }

        #endregion
    }
}
