using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class KeybindBoxTests
{
    static KeybindBoxTests()
    {
        if (Application.Current == null)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
    }

    public KeybindBoxTests()
    {
        SetDefaults();
    }

    [Fact]
    public void KeybindBox_Defaults_HasEmptyCollections()
    {
        var box = new KeybindBox();

        Assert.NotNull(box.Keybinds);
        Assert.Empty(box.Keybinds);
        Assert.NotNull(box.Tags);
        Assert.Empty(box.Tags);
        Assert.Equal(2, box.MaxTags);
    }

    [Fact]
    public void KeybindBox_AddKeybind_UpdatesTags()
    {
        var box = new KeybindBox();
        var keybind = new Keybind(Key.F5);

        box.Keybinds.Add(keybind);

        Assert.Single(box.Keybinds);
        Assert.Single(box.Tags);
        Assert.Equal("F5", box.Tags[0]);
    }

    [Fact]
    public void KeybindBox_AddTwoKeybinds_ShowsBothInTags()
    {
        var box = new KeybindBox();
        var k1 = new Keybind(Key.P, KeyModifiers.Control);
        var k2 = new Keybind(Key.O, KeyModifiers.Control | KeyModifiers.Shift);

        box.Keybinds.Add(k1);
        box.Keybinds.Add(k2);

        Assert.Equal(2, box.Keybinds.Count);
        Assert.Equal(2, box.Tags.Count);
        Assert.Equal(k1.ToString(), box.Tags[0]);
        Assert.Equal(k2.ToString(), box.Tags[1]);
    }

    [Fact]
    public void KeybindBox_WhenMaxTagsReached_AddingTagThrowsInvalidOperationException()
    {
        var box = new KeybindBox();
        box.Keybinds.Add(new Keybind(Key.A));
        box.Keybinds.Add(new Keybind(Key.B));

        Assert.NotNull(box.TagBox);
        Assert.True(box.TagBox.IsAtMax);

        Assert.Throws<InvalidOperationException>(() =>
        {
            box.Tags.Add("C");
        });

        Assert.Equal(2, box.Tags.Count);
        Assert.Equal(2, box.Keybinds.Count);
    }

    [Fact]
    public void KeybindBox_WhenMaxTagsReached_AddingKeybindThrowsInvalidOperationException()
    {
        var box = new KeybindBox();
        box.Keybinds.Add(new Keybind(Key.A));
        box.Keybinds.Add(new Keybind(Key.B));

        Assert.NotNull(box.TagBox);
        Assert.True(box.TagBox.IsAtMax);

        Assert.Throws<InvalidOperationException>(() =>
        {
            box.Keybinds.Add(new Keybind(Key.C));
        });

        Assert.Equal(2, box.Tags.Count);
        Assert.Equal(2, box.Keybinds.Count);
    }

    [Fact]
    public void KeybindBox_SetCollectionWithMoreThanTwoKeybinds_ThrowsArgumentException()
    {
        var box = new KeybindBox();
        var three = new ObservableCollection<Keybind>
        {
            new(Key.A),
            new(Key.B),
            new(Key.C)
        };

        Assert.Throws<ArgumentException>(() =>
        {
            box.Keybinds = three;
        });
    }

    [Fact]
    public void KeybindBox_RemoveKeybind_RemovesTag()
    {
        var box = new KeybindBox();
        var k1 = new Keybind(Key.A, KeyModifiers.Control);
        var k2 = new Keybind(Key.B, KeyModifiers.Alt);

        box.Keybinds.Add(k1);
        box.Keybinds.Add(k2);

        box.Keybinds.Remove(k1);

        Assert.Single(box.Keybinds);
        Assert.Single(box.Tags);
        Assert.Equal(k2.ToString(), box.Tags[0]);
    }

    [Fact]
    public void KeybindBox_RemoveTag_RemovesKeybind()
    {
        var box = new KeybindBox();
        var k1 = new Keybind(Key.A, KeyModifiers.Control);
        var k2 = new Keybind(Key.B, KeyModifiers.Alt);

        box.Keybinds.Add(k1);
        box.Keybinds.Add(k2);

        box.Tags.Remove(k1.ToString());

        Assert.Single(box.Keybinds);
        Assert.Equal(k2, box.Keybinds[0]);
        Assert.Single(box.Tags);
        Assert.Equal(k2.ToString(), box.Tags[0]);
    }

    [Fact]
    public void KeybindBox_ClearKeybinds_ClearsTags()
    {
        var box = new KeybindBox();
        box.Keybinds.Add(new Keybind(Key.A));
        box.Keybinds.Add(new Keybind(Key.B));

        box.Keybinds.Clear();

        Assert.Empty(box.Keybinds);
        Assert.Empty(box.Tags);
    }

    [Fact]
    public void KeybindBox_ClearTags_ClearsKeybinds()
    {
        var box = new KeybindBox();
        box.Keybinds.Add(new Keybind(Key.A));
        box.Keybinds.Add(new Keybind(Key.B));

        box.Tags.Clear();

        Assert.Empty(box.Keybinds);
        Assert.Empty(box.Tags);
    }

    [Fact]
    public void KeybindBox_InWindow_DisplaysTagsInTagBox_AndRemovesViaTagBox()
    {
        var box = new KeybindBox();
        var k1 = new Keybind(Key.A, KeyModifiers.Control);
        var k2 = new Keybind(Key.B, KeyModifiers.Shift);
        box.Keybinds.Add(k1);
        box.Keybinds.Add(k2);

        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(box.TagBox);
        Assert.NotNull(box.TagBox.Tags);
        Assert.Equal(2, box.TagBox.Tags.Count);
        Assert.Equal(k1.ToString(), box.TagBox.Tags[0]);
        Assert.Equal(k2.ToString(), box.TagBox.Tags[1]);
        Assert.Equal(2, box.TagBox.MaxTags);
        Assert.True(box.TagBox.IsAtMax);
        Assert.Contains(TagBox.Max, box.TagBox.Classes);

        // Remove tag via TagBox
        box.TagBox.RemoveTag(k1.ToString());
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.TagBox.Tags);
        Assert.Equal(k2.ToString(), box.TagBox.Tags[0]);
        Assert.Single(box.Keybinds);
        Assert.Equal(k2, box.Keybinds[0]);
        Assert.False(box.TagBox.IsAtMax);
        Assert.DoesNotContain(TagBox.Max, box.TagBox.Classes);
    }

    [Fact]
    public void KeybindBox_WhenMaxReached_TagBoxCursorIsNo_AndClickingDoesNotShowPresenter()
    {
        var box = new KeybindBox { PlaceholderText = "Press key..." };
        box.Keybinds.Add(new Keybind(Key.A));
        box.Keybinds.Add(new Keybind(Key.B));

        var button = new Button { Content = "Other" };
        var panel = new StackPanel();
        panel.Children.Add(box);
        panel.Children.Add(button);

        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        button.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(box.TagBox);
        Assert.True(box.TagBox.IsAtMax);
        Assert.Contains(TagBox.Max, box.TagBox.Classes);

        // Click KeybindBox while at max
        box.RaiseEvent(new PointerPressedEventArgs(
            box,
            new Pointer(0, PointerType.Mouse, true),
            window,
            new Point(5, 5),
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None));
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(box.TagBox.Presenter);
        Assert.False(box.TagBox.Presenter.IsVisible);

        var border = box.TagBox.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "PART_Border");
        Assert.NotNull(border);
        Assert.Equal("No", border.Cursor?.ToString());
    }
}
