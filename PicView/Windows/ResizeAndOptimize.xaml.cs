using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static PicView.Fields;

namespace PicView.Windows
{
    public partial class ResizeAndOptimize : Window
    {
        int rotation, quality, width, height, total;
        bool rename, aspectRatio, optimize, flip, resize = false;
        string name, destinationFolder, sourceFolder;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

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
            if (StartButton.Content.ToString() == "Stop")
            {
                StartButton.Content = "Cancelled";
                cts.Cancel();
            }

            string file = Pics[FolderIndex];

            UIprogressbar.Value = 0;

            GatherData();

            if (AllRadio.IsChecked.Value)
            {
                OutputlogBox.Text = "Starting!" + Environment.NewLine;
                var progress = new Progress<string>();
                progress.ProgressChanged += Progress_ProgressChanged;

                var currentFolder = Path.GetDirectoryName(Pics[FolderIndex]);
                List<string> tempFileList;

                if (currentFolder != sourceFolder)
                {
                    tempFileList = FileLists.FileList(sourceFolder);
                }
                else
                {
                    tempFileList = Pics;
                }
                total = tempFileList.Count;

                StartButton.Content = "Stop";

                await ImageDecoder.TransformImagesAsync(tempFileList, progress, cts.Token, resize, width, height, aspectRatio, rotation, quality, optimize, flip, name, destinationFolder).ConfigureAwait(false);
                return;
            }

            await Task.Run(() => ImageDecoder.TransformImage(file, resize, width, height, aspectRatio, rotation, quality, optimize, flip, name, destinationFolder)).ConfigureAwait(false);
        }

        private void Progress_ProgressChanged(object sender, string e)
        {
            UIprogressbar.Value = UIprogressbar.Value * 100 / total;

            OutputlogBox.Text += e + Environment.NewLine;

            //for (int i = 0; i < results.Count; i++)
            //{
            //    OutputlogBox.Text += results[i] + Environment.NewLine;
            //}

            //await mainWindow.Dispatcher.BeginInvoke((Action)(() =>
            //{
            //    OutputlogBox.Text += s + Environment.NewLine;
            //    UIprogressbar.Value = UIprogressbar.Value * 100 / total;
            //}));
        }

        private void GatherData()
        {
            if (!string.IsNullOrWhiteSpace(RotBox.Text))
            {
                _ = int.TryParse(RotBox.Text, out rotation);
            }
            else
            {
                rotation = 0;
            }

            if (!string.IsNullOrWhiteSpace(QualityBox.Text))
            {
                _ = int.TryParse(QualityBox.Text, out quality);
            }
            else
            {
                quality = 100;
            }

            if (!string.IsNullOrWhiteSpace(DestinationBox.Text))
            {
                destinationFolder = DestinationBox.Text;
            }
            else
            {
                destinationFolder = Path.GetDirectoryName(Pics[FolderIndex]);
            }

            if (!string.IsNullOrWhiteSpace(SourceBox.Text))
            {
                sourceFolder = SourceBox.Text;
            }
            else
            {
                sourceFolder = Path.GetDirectoryName(Pics[FolderIndex]);
            }

            //if (RenameCheckBox.IsChecked.Value)
            //{
            //    rename = true;
            //    name = Path.GetFileName(file);
            //}
            //else
            //{
            //    name = Path.GetFileName(file);
            //}

            if (!string.IsNullOrWhiteSpace(WidthValueBox.Text))
            {
                _ = int.TryParse(WidthValueBox.Text, out width);
                resize = true;
            }
            else
            {
                _ = int.TryParse(WidthBoxText.Text, out width);
            }

            if (!string.IsNullOrWhiteSpace(HeightValueBox.Text))
            {
                _ = int.TryParse(HeightValueBox.Text, out height);
                resize = true;
            }
            else
            {
                _ = int.TryParse(HeightBoxText.Text, out height);
            }

            aspectRatio = AspectRatioBox.IsChecked.Value;
            optimize = OptimizeBox.IsChecked.Value;
            flip = FlipBox.IsChecked.Value;
        }

        internal void UpdateValues()
        {
            RenameBoxText.Text = Path.GetFileName(Pics[FolderIndex]);
            RenameBoxText.Text += " [Not implemented]";
            DestinationBoxText.Text = SourceBoxText.Text = Path.GetDirectoryName(Pics[FolderIndex]);

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
