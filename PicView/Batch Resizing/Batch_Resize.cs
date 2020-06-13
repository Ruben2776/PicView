using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PicView.Fields;
using static PicView.LoadWindows;

namespace PicView
{
    internal static class Batch_Resize
    {
        static int rotation, quality, width, height;
        static bool rename, aspectRatio, optimize, flip, resize = false;
        private static string name;
        private static string destinationFolder;
        private static string sourceFolder;
        private static CancellationTokenSource cts = new CancellationTokenSource();

        internal static async Task StartProcessing()
        {
            if (resizeAndOptimize.StartButton.Content.ToString() == "Stop")
            {
                resizeAndOptimize.StartButton.Content = "Cancelled";
                cts.Cancel();
            }

            string file = Pics[FolderIndex];

            resizeAndOptimize.UIprogressbar.Value = 0;

            GatherData();

            if (resizeAndOptimize.AllRadio.IsChecked.Value)
            {
                resizeAndOptimize.OutputlogBox.Text = "Starting!" + Environment.NewLine;
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

                resizeAndOptimize.UIprogressbar.Maximum = tempFileList.Count;

                resizeAndOptimize.StartButton.Content = "Stop";

                await ImageDecoder.TransformImagesAsync(tempFileList, progress, cts.Token, resize, width, height, aspectRatio, rotation, quality, optimize, flip, name, destinationFolder).ConfigureAwait(false);
                return;
            }

            await Task.Run(() => ImageDecoder.TransformImage(file, resize, width, height, aspectRatio, rotation, quality, optimize, flip, name, destinationFolder)).ConfigureAwait(false);
        }

        private static void Progress_ProgressChanged(object sender, string e)
        {
            resizeAndOptimize.UIprogressbar.Value++;

            var sb = new StringBuilder(50);
            sb.Append(e).Append(Environment.NewLine);

            if (resizeAndOptimize.UIprogressbar.Value == resizeAndOptimize.UIprogressbar.Maximum)
            {
                sb.Append(Environment.NewLine).Append(" Finnished");
                resizeAndOptimize.StartButton.Content = "Start";
            }

            resizeAndOptimize.OutputlogBox.Text += sb.ToString();
        }

        internal static void GatherData()
        {
            if (!string.IsNullOrWhiteSpace(resizeAndOptimize.RotBox.Text))
            {
                _ = int.TryParse(resizeAndOptimize.RotBox.Text, out rotation);
            }
            else
            {
                rotation = 0;
            }

            if (!string.IsNullOrWhiteSpace(resizeAndOptimize.QualityBox.Text))
            {
                _ = int.TryParse(resizeAndOptimize.QualityBox.Text, out quality);
            }
            else
            {
                quality = 100;
            }

            if (!string.IsNullOrWhiteSpace(resizeAndOptimize.DestinationBox.Text))
            {
                destinationFolder = resizeAndOptimize.DestinationBox.Text;
            }
            else
            {
                destinationFolder = Path.GetDirectoryName(Pics[FolderIndex]);
            }

            if (!string.IsNullOrWhiteSpace(resizeAndOptimize.SourceBox.Text))
            {
                sourceFolder = resizeAndOptimize.SourceBox.Text;
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

            if (!string.IsNullOrWhiteSpace(resizeAndOptimize.WidthValueBox.Text))
            {
                _ = int.TryParse(resizeAndOptimize.WidthValueBox.Text, out width);
                resize = true;
            }
            else
            {
                _ = int.TryParse(resizeAndOptimize.WidthBoxText.Text, out width);
            }

            if (!string.IsNullOrWhiteSpace(resizeAndOptimize.HeightValueBox.Text))
            {
                _ = int.TryParse(resizeAndOptimize.HeightValueBox.Text, out height);
                resize = true;
            }
            else
            {
                _ = int.TryParse(resizeAndOptimize.HeightBoxText.Text, out height);
            }

            aspectRatio = resizeAndOptimize.AspectRatioBox.IsChecked.Value;
            optimize = resizeAndOptimize.OptimizeBox.IsChecked.Value;
            flip = resizeAndOptimize.FlipBox.IsChecked.Value;
        }

        internal static void UpdateValues()
        {
            resizeAndOptimize.RenameBoxText.Text = Path.GetFileName(Pics[FolderIndex]);
            resizeAndOptimize.RenameBoxText.Text += " [Not implemented]";
            resizeAndOptimize.DestinationBoxText.Text = resizeAndOptimize.SourceBoxText.Text = Path.GetDirectoryName(Pics[FolderIndex]);

            var dimensions = ImageDecoder.ImageSize(Pics[FolderIndex], true, true);

            if (dimensions.HasValue)
            {
                resizeAndOptimize.WidthBoxText.Text = dimensions.Value.Width.ToString(CultureInfo.CurrentCulture);
                resizeAndOptimize.HeightBoxText.Text = dimensions.Value.Height.ToString(CultureInfo.CurrentCulture);
            }
        }


    }
}
