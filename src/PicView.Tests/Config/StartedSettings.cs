using PicView.Core.ViewModels;

namespace PicView.Tests.Config;

/// <summary>
/// A <see cref="GlobalSettingsViewModel"/> with both write-through groups started, for tests that expect a stored write.
/// </summary>
internal static class StartedSettings
{
    internal static GlobalSettingsViewModel New(Func<ValueTask<bool>>? save = null)
    {
        var settings = new GlobalSettingsViewModel(save ?? (static () => ValueTask.FromResult(true)));
        settings.InitializeActionSettings();
        settings.InitializeExtraSettings();
        return settings;
    }
}
