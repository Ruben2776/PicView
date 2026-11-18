using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

/// <summary>
/// A <see cref="GallerySharedSettingsViewModel"/> with its write-throughs and commands started, for tests that expect a
/// stored write.
/// </summary>
internal static class StartedGallerySettings
{
    internal static GallerySharedSettingsViewModel New(Func<ValueTask<bool>>? save = null)
    {
        var settings = new GallerySharedSettingsViewModel(save ?? (static () => ValueTask.FromResult(true)));
        settings.Start();
        return settings;
    }
}
