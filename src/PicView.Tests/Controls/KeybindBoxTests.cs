using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class KeybindBoxTests
{
    public KeybindBoxTests()
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

    [Fact]
    public void KeybindBox_Defaults_HasEmptyCollections()
    {
        var box = new KeybindBox();

        Assert.NotNull(box.Keybinds);
        Assert.Empty(box.Keybinds);
        Assert.NotNull(box.Tags);
        Assert.Empty(box.Tags);
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
    public void KeybindBox_AddMoreThanTwoKeybinds_ThrowsInvalidOperationException()
    {
        var box = new KeybindBox();
        box.Keybinds.Add(new Keybind(Key.A));
        box.Keybinds.Add(new Keybind(Key.B));

        Assert.Throws<InvalidOperationException>(() =>
        {
            box.Keybinds.Add(new Keybind(Key.C));
        });

        Assert.Equal(2, box.Keybinds.Count);
        Assert.Equal(2, box.Tags.Count);
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

        // Remove tag via TagBox
        box.TagBox.RemoveTag(k1.ToString());
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.TagBox.Tags);
        Assert.Equal(k2.ToString(), box.TagBox.Tags[0]);
        Assert.Single(box.Keybinds);
        Assert.Equal(k2, box.Keybinds[0]);
    }

    [Fact]
    public void KeybindCollection_ConstructorsAndLimits_BehaveCorrectly()
    {
        var k1 = new Keybind(Key.X);
        var k2 = new Keybind(Key.Y);
        var k3 = new Keybind(Key.Z);

        var col = new KeybindCollection([k1, k2]);
        Assert.Equal(2, col.Count);

        Assert.Throws<InvalidOperationException>(() => col.Add(k3));
        Assert.Throws<ArgumentException>(() => new KeybindCollection(new List<Keybind> { k1, k2, k3 }));
        Assert.Throws<ArgumentException>(() => new KeybindCollection([k1, k2, k3]));
    }
}
