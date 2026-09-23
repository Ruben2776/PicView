using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class TagBoxTests
{
    public TagBoxTests()
    {
        try
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
        catch (InvalidOperationException)
        {
        }

        SetDefaults();
    }

    private static DispatcherTimer? GetCaretTimer(TextPresenter? presenter)
    {
        if (presenter is null)
        {
            return null;
        }

        var timerField = typeof(TextPresenter).GetField("_caretTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        return timerField?.GetValue(presenter) as DispatcherTimer;
    }

    [Fact]
    public void TagBox_WithTags_ShowsTagsAndRemovesTag()
    {
        var tagBox = new TagBox
        {
            Tags = new ObservableCollection<string> { "Ctrl", "Shift", "P" }
        };
        var window = new Window { Content = tagBox };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Tags should have 3 items
        Assert.Equal(3, tagBox.Tags.Count);

        // Remove a tag
        tagBox.RemoveTag("Shift");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, tagBox.Tags.Count);
        Assert.DoesNotContain("Shift", tagBox.Tags);
    }

    [Fact]
    public void TagBox_EmptyPseudoClass_UpdatesCorrectly()
    {
        var tagBox = new TagBox();
        var window = new Window { Content = tagBox };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(TagBox.Empty, tagBox.Classes);

        tagBox.Tags.Add("Alt");
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain(TagBox.Empty, tagBox.Classes);

        tagBox.RemoveTag("Alt");
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(TagBox.Empty, tagBox.Classes);
    }

    [Fact]
    public void TagBox_WhenFocused_StartsCaretBlinking_AndStopsOnLostFocus()
    {
        var tagBox = new TagBox { PlaceholderText = "Press key..." };
        var button = new Button { Content = "Other", Focusable = true };
        var panel = new StackPanel();
        panel.Children.Add(tagBox);
        panel.Children.Add(button);

        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(tagBox.Presenter);

        // Focus TagBox
        tagBox.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.True(tagBox.IsFocused);
        var timer = GetCaretTimer(tagBox.Presenter);
        Assert.NotNull(timer);
        Assert.True(timer.IsEnabled);

        // Move focus away to button
        button.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.False(tagBox.IsFocused);
        Assert.False(timer.IsEnabled);
    }

    [Fact]
    public void TagBox_OnPointerPressed_FocusesAndShowsCaret()
    {
        var tagBox = new TagBox { PlaceholderText = "Press key..." };
        var button = new Button { Content = "Other" };
        var panel = new StackPanel();
        panel.Children.Add(tagBox);
        panel.Children.Add(button);

        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Initially focus the button
        button.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.False(tagBox.IsFocused);

        // Simulate pointer press on TagBox
        tagBox.RaiseEvent(new PointerPressedEventArgs(
            tagBox,
            new global::Avalonia.Input.Pointer(0, PointerType.Mouse, true),
            window,
            new Point(5, 5),
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None));
        Dispatcher.UIThread.RunJobs();

        Assert.True(tagBox.IsFocused);
        var timer = GetCaretTimer(tagBox.Presenter);
        Assert.NotNull(timer);
        Assert.True(timer.IsEnabled);
    }

    [Fact]
    public void KeybindBox_OnPointerPressed_FocusesTagBoxAndShowsCaret()
    {
        var keybindBox = new KeybindBox { PlaceholderText = "Press key..." };
        var button = new Button { Content = "Other" };
        var panel = new StackPanel();
        panel.Children.Add(keybindBox);
        panel.Children.Add(button);

        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        button.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(keybindBox.TagBox);
        Assert.False(keybindBox.TagBox.IsFocused);

        // Click KeybindBox
        keybindBox.RaiseEvent(new PointerPressedEventArgs(
            keybindBox,
            new global::Avalonia.Input.Pointer(0, PointerType.Mouse, true),
            window,
            new Point(5, 5),
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None));
        Dispatcher.UIThread.RunJobs();

        Assert.True(keybindBox.TagBox.IsFocused);
        var timer = GetCaretTimer(keybindBox.TagBox.Presenter);
        Assert.NotNull(timer);
        Assert.True(timer.IsEnabled);
    }
}
