using PicView.Core.Gallery;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins that settings saved while the dock position still had a Closed value load at the bottom (ADR-0011).
/// </summary>
public class DockPositionSettingsTests
{
    [Fact]
    public void EnsureSettings_DockPositionZero_LoadsAsBottom()
    {
        var settings = GetDefaults();
        settings.Gallery.DockPosition = (GalleryDockPosition)0;

        EnsureSettings(settings);

        Assert.Equal(GalleryDockPosition.Bottom, settings.Gallery.DockPosition);
    }
}
