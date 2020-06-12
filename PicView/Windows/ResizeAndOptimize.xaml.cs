using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
            SelectedRadioBorder.MouseLeftButtonDown += delegate { SelectedRadio.IsChecked = true; };
            AllRadioBorder.MouseLeftButtonDown += delegate { AllRadio.IsChecked = true; };

            AllRadio.Checked += delegate { SelectedRadio.IsChecked = false; };
            SelectedRadio.Checked += delegate { AllRadio.IsChecked = false; };

            //KeyDown += KeysDown;
            KeyUp += KeysUp;
            //Scroller.MouseWheel += Info_MouseWheel;

            StartButton.Click += StartButton_Click;

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); mainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            int rotation, quality, width, height;
            bool rotate, rename, resize = false;
            string name, destination;
            string file = Pics[FolderIndex];

            OutputlogBox.Text = "Starting!";

            if (!string.IsNullOrWhiteSpace(RotBox.Text))
            {
                rotate = int.TryParse(RotBox.Text, out rotation);
            }
            else
            {
                rotation = 0;
            }

            if (!string.IsNullOrWhiteSpace(QualityBox.Text))
            {
                quality = int.Parse(QualityBox.Text);
            }
            else
            {
                quality = 100;
            }

            if (!string.IsNullOrWhiteSpace(DestinationBox.Text))
            {
                destination = DestinationBox.Text;
            }
            else
            {
                destination = Path.GetDirectoryName(file);
            }

            if (RenameCheckBox.IsChecked.Value)
            {
                rename = true;
                name = Path.GetFileName(file);
            }
            else
            {
                name = Path.GetFileName(file);
            }

            if (!string.IsNullOrWhiteSpace(WidthValueBox.Text))
            {
                int.TryParse(WidthValueBox.Text, out width);
                resize = true;
            }
            else
            {
                int.TryParse(WidthBoxText.Text, out width);
            }

            if (!string.IsNullOrWhiteSpace(HeightValueBox.Text))
            {
                int.TryParse(HeightValueBox.Text, out height);
                resize = true;
            }
            else
            {
                int.TryParse(HeightBoxText.Text, out height);
            }
            OutputlogBox.Text += ImageDecoder.TransformImage(file, resize, width, height, AspectRatioBox.IsChecked.Value, rotation, quality, OptimizeBox.IsChecked.Value, FlipBox.IsChecked.Value, name, destination);


            //if (AllRadio.IsChecked.Value)
            //{
            //    var currentFolder = Path.GetDirectoryName(Pics[FolderIndex]);
            //    var destinationFolder = Path.GetDirectoryName(AllBox.Text);
            //    if (destinationFolder != null && destinationFolder.Length != 0)
            //    {
            //        if (currentFolder != destinationFolder)
            //        {
            //            var tempFileList = FileLists.FileList(destinationFolder);

            //            Parallel.For(0, tempFileList.Count, (i, state) =>
            //            {
            //                ExecuteLogic(tempFileList[i]);
            //            });
            //        }
            //        return;
            //    }

            //    Parallel.For(0, Pics.Count, (i, state) =>
            //    {
            //        ExecuteLogic(Pics[i]);
            //    });
            //    return;
            //}

            //ExecuteLogic(Pics[FolderIndex]);
        }

        internal void UpdateValues()
        {
            RenameBoxText.Text = Path.GetFileName(Pics[FolderIndex]);
            RenameBoxText.Text += " [Not implemented]";
            DestinationBoxText.Text = AllBoxText.Text = Path.GetDirectoryName(Pics[FolderIndex]);

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
