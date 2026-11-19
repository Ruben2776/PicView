using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Core.ColorHandling;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.ColorManagement;

/// <summary>
/// Each Main Window derives its two background brushes from its own Window Settings, on the UI thread.
/// </summary>
[Collection("Sequential")]
public class BackgroundManagerTests
{
    public BackgroundManagerTests()
    {
        SetDefaults();
        Settings.UIProperties.BgColorChoice = (int)BackgroundType.Transparent;
        Settings.UIProperties.IsConstrainBackgroundColorEnabled = false;
    }

    [AvaloniaFact]
    public async Task Brushes_AreMadeOnTheUiThread()
    {
        var settings = WindowSettings.FromRecord(Settings);
        using var follow = BackgroundManager.Follow(settings);
        var uiThread = Environment.CurrentManagedThreadId;
        var assignedOn = new List<int>();
        using var watch = settings.ImageBackground.Skip(1)
            .Subscribe(_ => assignedOn.Add(Environment.CurrentManagedThreadId));

        await Task.Run(() => settings.BackgroundChoice.Value = (int)BackgroundType.White).ConfigureAwait(true);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(settings.ImageBackground.Value).Color);
        Assert.Equal(Colors.Transparent,
            Assert.IsType<SolidColorBrush>(settings.ConstrainedImageBackground.Value).Color);
        Assert.NotEmpty(assignedOn);
        Assert.All(assignedOn, id => Assert.Equal(uiThread, id));
    }

    [AvaloniaFact]
    public void Follow_AChangePostedAndThenTheWindowCloses_PaintsNothingAndDoesNotThrow()
    {
        var settings = WindowSettings.FromRecord(Settings);
        using var follow = BackgroundManager.Follow(settings);
        var before = settings.ImageBackground.Value;

        // Joined rather than awaited, so the UI thread runs nothing it was posted until the window has closed
        var other = new Thread(() => settings.BackgroundChoice.Value = (int)BackgroundType.White);
        other.Start();
        other.Join();
        settings.Dispose();
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(before);
        Assert.Same(before, settings.ImageBackground.Value);
    }
}
