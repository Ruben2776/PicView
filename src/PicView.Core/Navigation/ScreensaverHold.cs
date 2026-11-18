using PicView.Core.IPlatform;

namespace PicView.Core.Navigation;

/// <summary>
/// Keeps the screensaver off while any Main Window holds it, as a running slideshow does: the first hold turns it off
/// and releasing the last turns it back on. Call on the UI thread, since Windows keeps the setting per thread.
/// </summary>
public sealed class ScreensaverHold(IPlatformSpecificService? platform)
{
    private int _count;

    public bool IsHeld => _count > 0;

    public void Take()
    {
        if (_count++ == 0)
        {
            platform?.DisableScreensaver();
        }
    }

    /// <summary>
    /// Releases one hold. Releasing with nothing held does nothing.
    /// </summary>
    public void Release()
    {
        if (_count == 0)
        {
            return;
        }

        if (--_count == 0)
        {
            platform?.EnableScreensaver();
        }
    }
}
