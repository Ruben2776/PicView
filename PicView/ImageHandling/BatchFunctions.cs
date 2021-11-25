using ImageMagick;
using PicView.FileHandling;
using PicView.UILogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PicView.ImageHandling
{
    internal static class BatchFunctions
    {
        internal static async Task RunAsync(List<string> sourceFileist, int resizeAmount, int quality,
            Percentage? percentage, bool? compress, string outputFolder, bool toResize, TextBox LogTextBox,
            ProgressBar progressBar) => await Task.Run(() =>
        {
            Parallel.For(0, sourceFileist.Count, async i =>
            {
                var sourceFile = new FileInfo(sourceFileist[i]);
                var destination = outputFolder + sourceFile.Name;
                var sb = new StringBuilder();

                if (toResize)
                {
                    _ = doResize(LogTextBox, progressBar, sb, sourceFile, resizeAmount, quality, percentage, destination, compress).ConfigureAwait(false);
                }
                else if (compress.HasValue)
                {
                    if (sourceFile.DirectoryName == outputFolder)
                    {
                        await ImageFunctions.OptimizeImageAsync(sourceFile.FullName).ConfigureAwait(false);
                        var destinationFile = new FileInfo(destination);
                        sb.Append(sourceFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ').Append(FileFunctions.GetSizeReadable(sourceFile.Length))
                            .Append(" 🠚 ").Append(sourceFile.Name).Append(' ').Append(FileFunctions.GetSizeReadable(sourceFile.Length)).AppendLine(Environment.NewLine);
                    }
                    else
                    {

                    }

                    report(LogTextBox, progressBar, sb);
                }
            });
        });

        static async Task doResize(TextBox LogTextBox, ProgressBar progressBar, StringBuilder sb, FileInfo? sourceFile, int resizeAmount, int quality = 100, Percentage? percentage = null, string? destination = null, bool? compress = null)
        {
            var success = await ImageSizeFunctions.ResizeImageAsync(sourceFile.FullName, resizeAmount, resizeAmount, quality, percentage, destination, compress).ConfigureAwait(false);
            if (success is false) { return; }

            var destinationFile = new FileInfo(destination);
            sb.Append(sourceFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ').Append(FileFunctions.GetSizeReadable(sourceFile.Length)).Append(" 🠚 ")
                .Append(destinationFile.DirectoryName).Append('/').Append(sourceFile.Name).Append(' ').Append(FileFunctions.GetSizeReadable(destinationFile.Length)).
                Append(' ').AppendLine(Environment.NewLine);

            report(LogTextBox, progressBar, sb);
        }

        static void report(TextBox LogTextBox, ProgressBar progressBar, StringBuilder sb)
        {
            ConfigureWindows.GetResizeWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                LogTextBox.Text += sb.ToString();
                progressBar.Value++;
                LogTextBox.ScrollToEnd();

                if (progressBar.Value == progressBar.Maximum)
                {
                    LogTextBox.Text += Environment.NewLine + "Completed";
                }
            });
        }

        internal class ThumbNailHolder
        {
            internal string directory;
            internal int size;
            internal Percentage? percentage;

            internal ThumbNailHolder(string directory, int size, Percentage? percentage)
            {
                this.directory = directory;
                this.size = size;
                this.percentage = percentage;
            }
        }
    }
}
