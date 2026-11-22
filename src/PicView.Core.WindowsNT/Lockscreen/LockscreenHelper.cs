using Windows.Storage;
using Windows.System.UserProfile;
using PicView.Core.DebugTools;

namespace PicView.Core.WindowsNT.Lockscreen;

public static class LockscreenHelper
{
    /// <summary>
    /// Sets the file at <paramref name="path"/> as the current user's lock screen image.
    /// </summary>
    /// <remarks>
    /// This is the same per-user setting the Settings app writes, so the user can change the image there afterwards.
    /// It does not write the PersonalizationCSP registry values: those are a device policy, which locks the setting
    /// behind "Some settings are managed by your organization" and needs administrator rights.
    /// The call waits for Windows and is slow; run it on the thread pool.
    /// </remarks>
    /// <returns><see langword="false"/> when Windows refused the image, such as a format it cannot decode or a
    /// lock screen that a policy manages.</returns>
    public static async Task<bool>  SetLockScreenImageAsync(string path)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            await LockScreen.SetImageFileAsync(file);
            return true;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(LockscreenHelper), nameof(SetLockScreenImageAsync), e);
            return false;
        }
    }
}
