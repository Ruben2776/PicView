using PicView.ImageHandling;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace PicView.SystemIntegration
{
    internal static class Lockscreen
    {
        internal static async Task ChangeLockScreenBackground(string path)
        {

            if (UILogic.ConfigureWindows.GetMainWindow.MainImage.Effect != null || Clipboard.ContainsImage())
            {
                try
                {
                    var SaveImage = ImageDecoder.GetRenderedMagickImage();
                    if (SaveImage == null) { return; }

                    await UILogic.Tooltip.ShowTooltipMessage(Application.Current.Resources["Applying"]).ConfigureAwait(false);

                    await Task.Run(() =>
                    {
                        // Create temp directory
                        var tempPath = Path.GetTempPath();
                        var randomName = Path.GetRandomFileName() + ".png";

                        // Write temp file to it
                        using var filestream = new FileStream(tempPath + randomName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.SequentialScan);
                        SaveImage.Write(filestream);
                        SaveImage.Dispose();
                        filestream.Close();

                        // Use it
                        _ = Apply(tempPath + randomName);

                        // Clean up
                        File.Delete(tempPath + randomName);
                        using var timer = new Timer(2000);
                        timer.Elapsed += (s, x) => Directory.Delete(tempPath);
                    }).ConfigureAwait(true);

                    SaveImage.Dispose();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            else
            {
                await Apply(path).ConfigureAwait(false);
            }
        }

        private static async Task Apply(string path)
        {
            // Windows SDK Contracts error:
            // SupportedOSPlatformVersion 10.0.19041.0 cannot be higher than TargetPlatformVersion 7.0.
            // TODO find new lock screen saving method

            //try
            //{
            //    var storageFile = await StorageFile.GetFileFromPathAsync(path);

            //    using var stream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);
            //    await LockScreen.SetImageStreamAsync(stream);
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //}
        }
    }
}