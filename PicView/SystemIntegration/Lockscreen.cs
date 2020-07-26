using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;

namespace PicView.SystemIntegration
{
    internal class Lockscreen
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
                StorageFolder sf = await StorageFolder.GetFolderFromPathAsync(folder);
                StorageFile imgFile = await sf.GetFileAsync(file);

                using var stream = await imgFile.OpenAsync(FileAccessMode.Read);
                await LockScreen.SetImageStreamAsync(stream);
            }
            catch
            {

            }
        }
    }
}