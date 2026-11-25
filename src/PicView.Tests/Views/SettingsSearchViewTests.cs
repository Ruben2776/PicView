using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Main;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.SettingsSearch;
using PicView.Core.ViewModels;
using PicView.Tests.Config;

namespace PicView.Tests.Views;

[Collection("Sequential")]
public class SettingsSearchViewTests
{
    public SettingsSearchViewTests()
    {
        SetDefaults();
        Assert.True(TranslationManager.LoadLanguage("en"));
    }

    private static (CoreViewModel Core, SettingsView View, Window Window) Create()
    {
        var core = new CoreViewModel(null, _ => new ImageModel(), StartedSettings.New());
        var main = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        core.MainWindows.MainWindows.Add(main);
        core.MainWindows.ActiveWindow.Value = main;
        core.SettingsViewModel = new SettingsViewModel(core.Translation, core.GlobalSettings!);
        var view = new SettingsView { DataContext = core };
        // Headless has no popup windows, so the suggestions open in a popup overlay layer. The theme's window has
        // none, and Avalonia keeps the switch for one internal.
        var layers = new VisualLayerManager { EnableOverlayLayer = true, Child = view };
        typeof(VisualLayerManager)
            .GetProperty("EnablePopupOverlayLayer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(layers, true);
        var window = new Window { Width = 1000, Height = 700, Content = layers };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (core, view, window);
    }

    private static Dictionary<SettingKey, Control> Rows(SettingsView view)
    {
        var rows = new Dictionary<SettingKey, Control>();
        foreach (var control in view.GetVisualDescendants().OfType<Control>())
        {
            if (SearchProperties.GetKey(control) is { } key)
            {
                Assert.True(rows.TryAdd(key, control), $"{key} is on two rows");
            }
        }

        return rows;
    }

    private static void Type(SettingsView view, string text)
    {
        var box = view.FindControl<TextBox>("FilterBox")!;
        box.Focus();
        box.Text = text;
        Dispatcher.UIThread.RunJobs();
    }

    private static void PressEnter(SettingsView view)
    {
        view.FindControl<TextBox>("FilterBox")!.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Enter
        });
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void EverySettingKey_IsOnOneRowOfThePage()
    {
        var (_, view, window) = Create();
        try
        {
            var rows = Rows(view);

            var missing = Enum.GetValues<SettingKey>().Where(key => !rows.ContainsKey(key)).ToList();
            Assert.True(missing.Count == 0, "No row for: " + string.Join(", ", missing));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Typing_OpensTheSuggestionsWithTheFirstSelected_AndLeavesThePageAlone()
    {
        var (_, view, window) = Create();
        try
        {
            var scrollViewer = view.FindControl<ScrollViewer>("ContentScrollViewer")!;
            var categories = view.FindControl<ListBox>("CategoriesListBox")!;
            var visibleBefore = categories.Items.OfType<ListBoxItem>().Count(x => x.IsVisible);

            Type(view, "zoom");

            Assert.True(view.FindControl<Popup>("SuggestionsPopup")!.IsOpen);
            Assert.Equal(0, view.FindControl<ListBox>("SuggestionsListBox")!.SelectedIndex);
            Assert.Equal(0, scrollViewer.Offset.Y);
            Assert.Equal(visibleBefore, categories.Items.OfType<ListBoxItem>().Count(x => x.IsVisible));
            Assert.All(Rows(view).Values, row => Assert.Equal(1, row.Opacity));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Enter_OnASettingSuggestion_GoesToItsRowAndEndsTheSearch()
    {
        var (core, view, window) = Create();
        try
        {
            Type(view, "zoom previewer");
            var suggestion = Assert.Single(core.SettingsViewModel!.Suggestions.Value);
            Assert.Equal(SettingKey.ZoomPreviewer, suggestion.Key);

            PressEnter(view);

            var row = Rows(view)[SettingKey.ZoomPreviewer];
            Assert.Contains("searchPulse", (IEnumerable<string>)row.Classes);
            Assert.Equal(SettingsCategory.Zoom, core.SettingsViewModel!.SelectedCategory.Value);
            Assert.True(view.FindControl<ScrollViewer>("ContentScrollViewer")!.Offset.Y > 0);

            var focused = Assert.IsAssignableFrom<Visual>(window.FocusManager!.GetFocusedElement());
            Assert.True(row.IsVisualAncestorOf(focused));

            Assert.Equal(string.Empty, core.SettingsViewModel!.SearchQuery.Value);
            Assert.False(view.FindControl<Popup>("SuggestionsPopup")!.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Enter_OnAnOptionOfASetting_GoesToThatSettingOnly()
    {
        var (core, view, window) = Create();
        try
        {
            Type(view, "navigate backwards");
            PressEnter(view);

            var rows = Rows(view);
            Assert.Contains("searchPulse", (IEnumerable<string>)rows[SettingKey.WhenDeletingFile].Classes);
            Assert.All(rows.Where(x => x.Key != SettingKey.WhenDeletingFile),
                x => Assert.DoesNotContain("searchPulse", (IEnumerable<string>)x.Value.Classes));
            Assert.Equal(SettingsCategory.General, core.SettingsViewModel!.SelectedCategory.Value);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Enter_OnACategorySuggestion_GoesToTheCategoryWithoutAPulse()
    {
        var (core, view, window) = Create();
        try
        {
            Type(view, "mouse");
            Assert.True(core.SettingsViewModel!.Suggestions.Value[0].IsCategory);

            PressEnter(view);

            Assert.Equal(SettingsCategory.Mouse, core.SettingsViewModel!.SelectedCategory.Value);
            Assert.All(Rows(view).Values, row => Assert.DoesNotContain("searchPulse", (IEnumerable<string>)row.Classes));
            Assert.Equal(string.Empty, core.SettingsViewModel!.SearchQuery.Value);
        }
        finally
        {
            window.Close();
        }
    }
}
