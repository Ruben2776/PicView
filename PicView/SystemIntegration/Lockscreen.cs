using PicView.ImageHandling;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Windows.Storage;
using Windows.System.UserProfile;

namespace PicView.SystemIntegration
{
    internal static class Lockscreen
    {
        internal static async Task ChangeLockScreenBackground(string path)
        {
            if (!UserProfilePersonalizationSettings.IsSupported())
            {
                return;
            }

            if (UILogic.Loading.LoadWindows.GetMainWindow.MainImage.Effect != null || Clipboard.ContainsImage())
            {
                try
                {
                    var SaveImage = ImageDecoder.GetRenderedMagickImage();
                    if (SaveImage == null) { return; }

                    UILogic.Tooltip.ShowTooltipMessage(Application.Current.Resources["Applying"]);

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
                        Apply(tempPath + randomName);

                        // Clean up
                        File.Delete(tempPath + randomName);
                        using var timer = new Timer(2000);
                        timer.Elapsed += (s, x) => Directory.Delete(tempPath);
                    }).ConfigureAwait(true);

                    SaveImage.Dispose(); // Make visual studio happy
                }
                catch { }
            }
            else
            {
                Apply(path);
            }
        }

        static async void Apply(string path)
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(path);

                using var stream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);
                await LockScreen.SetImageStreamAsync(stream);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}