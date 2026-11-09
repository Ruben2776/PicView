using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class RenameDialogTests
{
    public RenameDialogTests()
    {
        SetDefaults();
        TranslationManager.Init();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
    }

    [AvaloniaTheory]
    [InlineData("holiday photo.jpg", "holiday photo")]
    [InlineData("archive.tar.gz", "archive.tar")]
    [InlineData("README", "README")]
    public void Opening_SelectsTheNameWithoutTheExtension(string fileName, string selected)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()),
            new GlobalSettingsViewModel(static () => ValueTask.FromResult(true)));
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings, core.GallerySettings);
        vm.WindowTabs.ActiveTab.Value.FileInfo.Value = new FileInfo(Path.Combine(Path.GetTempPath(), fileName));
        var dialog = new RenameDialog { DataContext = vm };
        var window = new Window { Width = 900, Height = 600, Content = dialog };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(fileName, dialog.RenameBox.Text);
        Assert.Equal(0, dialog.RenameBox.SelectionStart);
        Assert.Equal(selected, dialog.RenameBox.SelectedText);
        window.Close();
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
