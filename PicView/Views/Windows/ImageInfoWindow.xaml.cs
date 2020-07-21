using PicView.Editing.HlslEffects;
using System;
using System.Windows;
using System.Windows.Input;
using static PicView.Library.Fields;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class ImageInfoWindow : Window
    {
        public ImageInfoWindow()
        {
            InitializeComponent();

            ContentRendered += Window_ContentRendered;


        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            KeyDown += KeysDown;
            KeyUp += KeysUp;

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); TheMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            Width -= 1; // fix rendering bug
            Height -= 1;

        }

        #region Keyboard Shortcuts

        private void KeysDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
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
                    Hide();
                    TheMainWindow.Focus();
                    break;

                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Environment.Exit(0);
                    }
                    break;
            }
        }

        #endregion Keyboard Shortcuts

    }
}