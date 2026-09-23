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
using MouseButton = PicView.Avalonia.Input.MouseButton;

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

    [Fact]
    public void KeybindBox_WhenTagBoxFocused_KeyPress_AddsTagOnKeyUp()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.TagBox.IsFocused);

        // KeyDown should not add yet
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.F,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(box.Keybinds);
        Assert.Empty(box.Tags);

        // KeyUp commits the keybind
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.F,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.Keybinds);
        Assert.Equal(Key.F, box.Keybinds[0].Key);
        Assert.Single(box.Tags);
        Assert.Equal("F", box.Tags[0]);
    }

    [Fact]
    public void KeybindBox_WhenTagBoxFocused_KeyCombinationWithModifiers_AddsTagOnKeyUp()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.TagBox.IsFocused);

        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.LeftCtrl,
            KeyModifiers = KeyModifiers.Control,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.P,
            KeyModifiers = KeyModifiers.Control,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.P,
            KeyModifiers = KeyModifiers.Control,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.Keybinds);
        Assert.Equal(new Keybind(Key.P, KeyModifiers.Control), box.Keybinds[0]);
        Assert.Single(box.Tags);
        Assert.Equal("Ctrl + P", box.Tags[0]);
    }

    [Fact]
    public void KeybindBox_WhenTagBoxFocused_ModifierReleasedBeforeKey_PreservesModifiers()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.LeftShift,
            KeyModifiers = KeyModifiers.Shift,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Z,
            KeyModifiers = KeyModifiers.Shift,
            Source = box.TagBox
        });
        // Modifier released first
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.LeftShift,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Assert.Empty(box.Keybinds);

        // Normal key released
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.Z,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.Keybinds);
        Assert.Equal(new Keybind(Key.Z, KeyModifiers.Shift), box.Keybinds[0]);
        Assert.Single(box.Tags);
        Assert.Equal("Shift + Z", box.Tags[0]);
    }

    [Fact]
    public void KeybindBox_WhenTagBoxFocused_ModifierOnly_DoesNotAddTag()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.LeftCtrl,
            KeyModifiers = KeyModifiers.Control,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.LeftCtrl,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(box.Keybinds);
        Assert.Empty(box.Tags);
    }

    [Fact]
    public void KeybindBox_WhenTagBoxFocused_MiddleMouseButton_AddsTag()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.TagBox.IsFocused);

        box.TagBox.RaiseEvent(new PointerPressedEventArgs(
            box.TagBox,
            new Pointer(0, PointerType.Mouse, true),
            window,
            new Point(5, 5),
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.MiddleButtonPressed),
            KeyModifiers.None));
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.Keybinds);
        Assert.Equal(new Keybind(MouseButton.Middle), box.Keybinds[0]);
        Assert.Single(box.Tags);
        Assert.Equal("Middle", box.Tags[0]);
    }

    [Fact]
    public void KeybindBox_WhenTagBoxFocused_SideMouseButtons_AddsTags()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.TagBox.IsFocused);

        // Click XButton1
        box.TagBox.RaiseEvent(new PointerPressedEventArgs(
            box.TagBox,
            new Pointer(0, PointerType.Mouse, true),
            window,
            new Point(5, 5),
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.XButton1Pressed),
            KeyModifiers.None));
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.Keybinds);
        Assert.Equal(new Keybind(MouseButton.XButton1), box.Keybinds[0]);
        Assert.Equal("XButton1", box.Tags[0]);

        // Click XButton2
        box.TagBox.RaiseEvent(new PointerPressedEventArgs(
            box.TagBox,
            new Pointer(0, PointerType.Mouse, true),
            window,
            new Point(5, 5),
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.XButton2Pressed),
            KeyModifiers.None));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, box.Keybinds.Count);
        Assert.Equal(new Keybind(MouseButton.XButton2), box.Keybinds[1]);
        Assert.Equal("XButton2", box.Tags[1]);
        Assert.True(box.TagBox.IsAtMax);
    }

    [Fact]
    public void KeybindBox_WhenTagBoxFocused_MouseButtonWithModifier_AddsTag()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.TagBox.IsFocused);

        var expectedKeybind = new Keybind(MouseButton.Middle, KeyModifiers.Alt);

        box.TagBox.RaiseEvent(new PointerPressedEventArgs(
            box.TagBox,
            new Pointer(0, PointerType.Mouse, true),
            window,
            new Point(5, 5),
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.MiddleButtonPressed),
            KeyModifiers.Alt));
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.Keybinds);
        Assert.Equal(expectedKeybind, box.Keybinds[0]);
        Assert.Single(box.Tags);
        Assert.Equal(expectedKeybind.ToString(), box.Tags[0]);
    }

    [Fact]
    public void KeybindBox_WhenTagBoxNotFocused_KeyPress_IsIgnored()
    {
        var box = new KeybindBox();
        var button = new Button { Content = "Other" };
        var panel = new StackPanel();
        panel.Children.Add(box);
        panel.Children.Add(button);

        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        button.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.False(box.TagBox!.IsFocused);

        box.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.None,
            Source = box
        });
        box.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.None,
            Source = box
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(box.Keybinds);
        Assert.Empty(box.Tags);
    }

    [Fact]
    public void KeybindBox_WhenTagBoxNotFocused_MiddleMouseButton_DoesNotAddKeybind()
    {
        var box = new KeybindBox();
        var button = new Button { Content = "Other" };
        var panel = new StackPanel();
        panel.Children.Add(box);
        panel.Children.Add(button);

        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        button.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.False(box.TagBox!.IsFocused);

        box.RaiseEvent(new PointerPressedEventArgs(
            box,
            new Pointer(0, PointerType.Mouse, true),
            window,
            new Point(5, 5),
            (ulong)Environment.TickCount64,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.MiddleButtonPressed),
            KeyModifiers.None));
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(box.Keybinds);
        Assert.Empty(box.Tags);
    }

    [Fact]
    public void KeybindBox_WhenDuplicateKeybindPressed_IgnoresDuplicate()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        // First press of Key.A
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();
        Assert.Single(box.Keybinds);

        // Second press of Key.A
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Single(box.Keybinds);
        Assert.Single(box.Tags);
    }

    [Fact]
    public void KeybindBox_WhenMaxTagsReached_DynamicKeyPressIgnored()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        // Add 1st key
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });

        // Add 2nd key
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.B,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.B,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, box.Keybinds.Count);
        Assert.True(box.TagBox.IsAtMax);

        // Attempt 3rd key
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.C,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.C,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, box.Keybinds.Count);
        Assert.Equal(2, box.Tags.Count);
    }

    [Fact]
    public void KeybindBox_WhenEscapePressed_ClearsFocus()
    {
        var box = new KeybindBox();
        var window = new Window { Content = box };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        box.TagBox!.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.TagBox.IsFocused);

        box.TagBox.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Escape,
            KeyModifiers = KeyModifiers.None,
            Source = box.TagBox
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(box.Keybinds);
        Assert.False(box.TagBox.IsFocused);
    }
}
