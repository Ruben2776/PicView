using PicView.FileHandling;
using PicView.ImageHandling;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Loading.LoadWindows;

namespace PicView.Editing
{
    internal static class Batch_Resize
    {
        private static int rotation, quality, width, height;
        private static bool rename, aspectRatio, optimize, flip, resize = false;
        private static string name;
        private static string destinationFolder;
        private static string sourceFolder;
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();

        internal static async Task StartProcessing()
        {
            if (GetResizeAndOptimize.StartButton.Content.ToString() == "Stop")
            {
                cts.Cancel();
                HandleCencalled();
                return;
            }

            string file = Pics[FolderIndex];

            GetResizeAndOptimize.UIprogressbar.Value = 0;

            GatherData();

            if (!GetResizeAndOptimize.AllRadio.IsChecked.Value)
            {
                await Task.Run(() =>
                    ImageDecoder.TransformImage(file, resize, width, height, aspectRatio, rotation, quality,
                        optimize, flip, name, destinationFolder)).ConfigureAwait(false);
                return;
            }

            GetResizeAndOptimize.StartButton.Content = "Stop";

            var progress = new Progress<string>();
            progress.ProgressChanged += Progress_ProgressChanged;

            var currentFolder = Path.GetDirectoryName(Pics[FolderIndex]);

            if (currentFolder != sourceFolder)
            {
                var tempFileList = FileLists.FileList(sourceFolder);
                GetResizeAndOptimize.UIprogressbar.Maximum = tempFileList.Count;
                try
                {
                    await TransformImagesAsync(tempFileList, progress, resize, width, height, aspectRatio, rotation, quality,
                    optimize, flip, name, destinationFolder, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return; }
            }
            else
            {
                GetResizeAndOptimize.UIprogressbar.Maximum = Pics.Count;

                await TransformImagesAsync(Pics, progress, resize, width, height, aspectRatio, rotation, quality,
                    optimize, flip, name, destinationFolder, cts.Token).ConfigureAwait(false);
            }
        }

        private static async void HandleCencalled()
        {
            await GetMainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                GetResizeAndOptimize.StartButton.Content = "Cancelled";
                GetResizeAndOptimize.UIprogressbar.Value = 0;
            }));
        }

        private static void Progress_ProgressChanged(object sender, string e)
        {
            GetResizeAndOptimize.UIprogressbar.Value++;

            var sb = new StringBuilder(50);
            sb.Append(e).Append(Environment.NewLine);

            if (GetResizeAndOptimize.UIprogressbar.Value == GetResizeAndOptimize.UIprogressbar.Maximum)
            {
                sb.Append(Environment.NewLine).Append(" Finnished");
                GetResizeAndOptimize.StartButton.Content = "Start";
            }

            GetResizeAndOptimize.OutputlogBox.Text += sb.ToString();
        }

        internal static void GatherData()
        {
            if (!string.IsNullOrWhiteSpace(GetResizeAndOptimize.RotBox.Text))
            {
                _ = int.TryParse(GetResizeAndOptimize.RotBox.Text, out rotation);
            }
            else
            {
                rotation = 0;
            }

            if (!string.IsNullOrWhiteSpace(GetResizeAndOptimize.QualityBox.Text))
            {
                _ = int.TryParse(GetResizeAndOptimize.QualityBox.Text, out quality);
            }
            else
            {
                quality = 100;
            }

            if (!string.IsNullOrWhiteSpace(GetResizeAndOptimize.DestinationBox.Text))
            {
                destinationFolder = GetResizeAndOptimize.DestinationBox.Text;
            }
            else
            {
                destinationFolder = Path.GetDirectoryName(Pics[FolderIndex]);
            }

            if (!string.IsNullOrWhiteSpace(GetResizeAndOptimize.SourceBox.Text))
            {
                sourceFolder = GetResizeAndOptimize.SourceBox.Text;
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

            if (!string.IsNullOrWhiteSpace(GetResizeAndOptimize.WidthValueBox.Text))
            {
                _ = int.TryParse(GetResizeAndOptimize.WidthValueBox.Text, out width);
                resize = true;
            }
            else
            {
                _ = int.TryParse(GetResizeAndOptimize.WidthBoxText.Text, out width);
            }

            if (!string.IsNullOrWhiteSpace(GetResizeAndOptimize.HeightValueBox.Text))
            {
                _ = int.TryParse(GetResizeAndOptimize.HeightValueBox.Text, out height);
                resize = true;
            }
            else
            {
                _ = int.TryParse(GetResizeAndOptimize.HeightBoxText.Text, out height);
            }

            aspectRatio = GetResizeAndOptimize.AspectRatioBox.IsChecked.Value;
            optimize = GetResizeAndOptimize.OptimizeBox.IsChecked.Value;
            flip = GetResizeAndOptimize.FlipBox.IsChecked.Value;
        }

        internal static void UpdateValues()
        {
            if (FolderIndex >= Pics.Count)
            {
                return;
            }

            GetResizeAndOptimize.RenameBoxText.Text = Path.GetFileName(Pics[FolderIndex]);
            GetResizeAndOptimize.RenameBoxText.Text += " [Not implemented]";
            GetResizeAndOptimize.DestinationBoxText.Text = GetResizeAndOptimize.SourceBoxText.Text = Path.GetDirectoryName(Pics[FolderIndex]);

            var dimensions = ImageDecoder.ImageSize(Pics[FolderIndex], true, true);

            if (dimensions.HasValue)
            {
                GetResizeAndOptimize.WidthBoxText.Text = dimensions.Value.Width.ToString(CultureInfo.CurrentCulture);
                GetResizeAndOptimize.HeightBoxText.Text = dimensions.Value.Height.ToString(CultureInfo.CurrentCulture);
            }
        }

        internal static async Task TransformImagesAsync(
            List<string> files, IProgress<string> progress, bool resize, int width, int height, bool aspectRatio,
            int rotation, int quality, bool optimize, bool flip, string name, string destination, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var x = ImageDecoder.TransformImage(files[i], resize, width, height, aspectRatio, rotation, quality, optimize, flip, name, destination);
                    progress.Report(x);
                    //cancellationToken.ThrowIfCancellationRequested();
                }
            }, cancellationToken).ConfigureAwait(false);

            //await Task.Run(() =>
            //Parallel.For(0, files.Count, (i, state) =>
            //{
            //    var x = ImageDecoder.TransformImage(files[i], resize, width, height, aspectRatio, rotation, quality, optimize, flip, name, destination);
            //    progress.Report(x);
            //    cancellationToken.ThrowIfCancellationRequested();

            //})).ConfigureAwait(false);
        }
    }
}