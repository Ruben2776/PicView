using PicView.UILogic.Loading;
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
            CloseButton.TheButton.Click += delegate { Hide(); LoadWindows.GetMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            KeyUp += (_,e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Hide();
                    LoadWindows.GetMainWindow.Focus();
                }
                else if (e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    Environment.Exit(0);
                }
            };
        }
    }
}