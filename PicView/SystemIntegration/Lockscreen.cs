using System;
using System.IO;
using System.Threading.Tasks;
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

            var folder = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path);

            try
            {
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(folder);
                var storageFile = await storageFolder.GetFileAsync(file);

                using var stream = await storageFile.OpenAsync(FileAccessMode.Read);
                await LockScreen.SetImageStreamAsync(stream);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}