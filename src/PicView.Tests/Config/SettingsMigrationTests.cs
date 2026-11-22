using PicView.Core.Config;

namespace PicView.Tests.Config;

public class SettingsMigrationTests
{
    private static AppSettings Version31(bool? openLastFile = null, bool? openInSameWindow = null)
    {
        var settings = new AppSettings
        {
            Version = 3.1,
            UIProperties = new UIProperties { OpenInSameWindow = openInSameWindow },
            StartUp = new PicView.Core.Config.StartUp { OpenLastFile = openLastFile }
        };
        return settings;
    }

    [Fact]
    public void EnsureSettings_FromVersion31_MapsOpenLastFileTrue_ToOpenLastFileBehavior()
    {
        var settings = SettingsManager.EnsureSettings(Version31(openLastFile: true));

        Assert.Equal(StartUpBehavior.OpenLastFile, settings.StartUp!.Behavior);
        Assert.Null(settings.StartUp.OpenLastFile);
    }

    [Fact]
    public void EnsureSettings_FromVersion31_MapsOpenLastFileFalse_ToNone()
    {
        var settings = SettingsManager.EnsureSettings(Version31(openLastFile: false));

        Assert.Equal(StartUpBehavior.None, settings.StartUp!.Behavior);
        Assert.Null(settings.StartUp.OpenLastFile);
    }

    [Fact]
    public void EnsureSettings_FromVersion31_MapsOpenInSameWindowTrue_ToSameWindow()
    {
        var settings = SettingsManager.EnsureSettings(Version31(openInSameWindow: true));

        Assert.Equal(OpeningMode.SameWindow, settings.UIProperties!.OpeningMode);
        Assert.Null(settings.UIProperties.OpenInSameWindow);
    }

    [Fact]
    public void EnsureSettings_FromVersion31_MapsOpenInSameWindowFalse_ToNewWindow()
    {
        var settings = SettingsManager.EnsureSettings(Version31(openInSameWindow: false));

        Assert.Equal(OpeningMode.NewWindow, settings.UIProperties!.OpeningMode);
        Assert.Null(settings.UIProperties.OpenInSameWindow);
    }

    [Fact]
    public void EnsureSettings_AtVersion32_KeepsBehaviorAndOpeningMode()
    {
        var settings = new AppSettings
        {
            Version = 3.2,
            UIProperties = new UIProperties { OpeningMode = OpeningMode.NewTab },
            StartUp = new PicView.Core.Config.StartUp { Behavior = StartUpBehavior.ContinueSession }
        };

        settings = SettingsManager.EnsureSettings(settings);

        Assert.Equal(StartUpBehavior.ContinueSession, settings.StartUp!.Behavior);
        Assert.Equal(OpeningMode.NewTab, settings.UIProperties!.OpeningMode);
    }

    [Fact]
    public void DefaultUIProperties_OpeningMode_IsSameWindowOnMacOsElseNewWindow()
    {
        var expected = OperatingSystem.IsMacOS() ? OpeningMode.SameWindow : OpeningMode.NewWindow;

        Assert.Equal(expected, SettingsManager.GetDefaultUIProperties().OpeningMode);
    }
}
