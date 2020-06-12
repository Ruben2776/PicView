using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using static PicView.Fields;

namespace PicView.Windows
{
    public partial class ResizeAndOptimize : Window
    {

        public ResizeAndOptimize()
        {
            InitializeComponent();

            ContentRendered += Window_ContentRendered;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //KeyDown += KeysDown;
            KeyUp += KeysUp;
            //Scroller.MouseWheel += Info_MouseWheel;



            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); mainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };
        }

        internal void UpdateValues()
        {
            RenameBoxText.Text = Path.GetFileName(Pics[FolderIndex]);
            MoveBoxText.Text = SpecBoxText.Text = Path.GetDirectoryName(Pics[FolderIndex]);

            var dimensions = ImageDecoder.ImageSize(Pics[FolderIndex], true, true);

            if (dimensions.HasValue)
            {
                WidthBoxText.Text = dimensions.Value.Width.ToString(CultureInfo.CurrentCulture);
                HeightBoxText.Text = dimensions.Value.Height.ToString(CultureInfo.CurrentCulture);
            }
        }


        #region Keyboard Shortcuts

        //private void KeysDown(object sender, KeyEventArgs e)
        //{
        //    switch (e.Key)
        //    {
        //        case Key.Down:
        //        case Key.PageDown:
        //        case Key.S:
        //            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + zoomSpeed);
        //            break;
        //        case Key.Up:
        //        case Key.PageUp:
        //        case Key.W:
        //            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - zoomSpeed);
        //            break;
        //        case Key.Q:
        //            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        //            {
        //                Environment.Exit(0);
        //            }
        //            break;
        //    }
        //}

        private void KeysUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    mainWindow.Focus();
                    break;
                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Environment.Exit(0);
                    }
                    break;

            }
        }

        //private void Info_MouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    if (e.Delta > 0)
        //    {
        //        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - zoomSpeed);
        //    }
        //    else if (e.Delta < 0)
        //    {
        //        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + zoomSpeed);
        //    }
        //}

        #endregion


    }
}
