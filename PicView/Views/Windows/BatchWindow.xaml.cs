using PicView.UILogic;
using System;
using System.Windows;
using System.Windows.Input;

namespace PicView.Views.Windows
{
    public partial class BatchWindow : Window
    {
        public BatchWindow()
        {
            InitializeComponent();

            ContentRendered += Window_ContentRendered;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); ConfigureWindows.GetMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            KeyDown += (_, e) => Shortcuts.GenericWindowShortcuts.KeysDown(null, e, this);
        }
    }
}