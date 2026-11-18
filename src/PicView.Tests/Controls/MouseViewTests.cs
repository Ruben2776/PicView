using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Views.Config;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Config;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class MouseViewTests
{
    public MouseViewTests()
    {
        SetDefaults();
    }

    private static LanguageModel Language(string suffix) => new()
    {
        File = "file",
        Files = "files",
        None = "None-" + suffix,
        ResetZoom = "ResetZoom-" + suffix,
        ToggleFullscreen = "ToggleFullscreen-" + suffix
    };

    [AvaloniaFact]
    public void LanguageChanges_DoubleClickBox_ShowsTheNewLanguageAndKeepsItsSelection()
    {
        var previous = TranslationManager.Swap(Language("a"));
        try
        {
            Settings.UIProperties.DoubleClickBehavior = 2;
            var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()), StartedSettings.New());
            core.Translation.UpdateLanguage();
            using var settings = new SettingsViewModel(core.Translation, core.GlobalSettings);
            core.SettingsViewModel = settings;
            var view = new MouseView { DataContext = core };
            var window = new Window { Width = 900, Height = 600, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var box = view.DoubleClickBox;
            Assert.Equal(2, box.SelectedIndex);
            Assert.Equal("ToggleFullscreen-a", ((ComboBoxItem)box.SelectedItem!).Content);

            TranslationManager.Swap(Language("b"));
            core.Translation.UpdateLanguage();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(["None-b", "ResetZoom-b", "ToggleFullscreen-b"],
                box.Items.Cast<ComboBoxItem>().Select(item => item.Content));
            Assert.Equal(2, box.SelectedIndex);
            Assert.Equal(2, settings.MouseDoubleClickBehaviorIndex.Value);
            window.Close();
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }
}
