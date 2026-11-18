using Avalonia;
using PicView.Avalonia.Services;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Config;

namespace PicView.Tests.Services;

[Collection("Sequential")]
public class ImageSettingsServiceTests
{
    public ImageSettingsServiceTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void TriggerScalingModeUpdate_NoActiveMainWindow_DoesNothing()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()), StartedSettings.New());
        var previous = Application.Current!.DataContext;
        Application.Current.DataContext = core;
        try
        {
            Assert.Null(core.MainWindows.ActiveWindow.CurrentValue);

            var exception = Record.Exception(() => new ImageSettingsService().TriggerScalingModeUpdate(true));

            Assert.Null(exception);
        }
        finally
        {
            Application.Current.DataContext = previous;
        }
    }
}
