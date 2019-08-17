
using PicView.Helpers;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PicView.Windows
{
    /// <summary>
    /// Interaction logic for YesNoDialogWindow.xaml
    /// </summary>
    public partial class Tools : Window
    {
        public Tools()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
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
        }

        #region Eventhandlers
        
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Closing -= Window_Closing;
            
            AnimationHelper.FadeWindow(this, 0, TimeSpan.FromSeconds(.5));
            FocusManager.SetFocusedElement(Application.Current.MainWindow, this);
        }        

        #endregion

        
    }
}
