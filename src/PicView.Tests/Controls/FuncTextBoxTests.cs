using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.Controls;

[Collection("Sequential")]
public class FuncTextBoxTests
{
    private sealed class CountingCommand : ICommand
    {
        public int Count { get; private set; }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => Count++;
    }

    private static (FuncTextBox Box, TextBox Other, CountingCommand Command) Create(bool acceptsReturn,
        bool commitOnLostFocus = false)
    {
        var command = new CountingCommand();
        var box = new FuncTextBox { Command = command, AcceptsReturn = acceptsReturn, Text = "a" };
        if (commitOnLostFocus)
        {
            box.CommitOnLostFocus = true;
        }

        var other = new TextBox();
        var panel = new StackPanel();
        panel.Children.Add(box);
        panel.Children.Add(other);
        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        box.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(box.IsFocused);
        return (box, other, command);
    }

    private static void PressKey(FuncTextBox box, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        box.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key,
            KeyModifiers = modifiers,
            Source = box
        });
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Enter_AcceptsReturn_DoesNotExecute()
    {
        var (box, _, command) = Create(true);

        PressKey(box, Key.Enter);

        Assert.Equal(0, command.Count);
        Assert.Contains('\n', box.Text);
    }

    [AvaloniaFact]
    public void CtrlEnter_AcceptsReturn_Executes()
    {
        var (box, _, command) = Create(true);

        PressKey(box, Key.Enter, KeyModifiers.Control);

        Assert.Equal(1, command.Count);
        Assert.Equal("a", box.Text);
    }

    [AvaloniaFact]
    public void Enter_SingleLine_Executes()
    {
        var (box, _, command) = Create(false);

        PressKey(box, Key.Enter);

        Assert.Equal(1, command.Count);
    }

    [AvaloniaFact]
    public void LostFocus_CommitOnLostFocus_Executes()
    {
        var (box, other, command) = Create(true, true);

        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.True(other.IsFocused);
        Assert.False(box.IsFocused);
        Assert.Equal(1, command.Count);
    }

    // A click on a typo in a long prompt places the caret there, as in any text box. A single-line box keeps its own
    // first click, which puts the caret at the end
    [AvaloniaTheory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void FirstClick_MultiLine_PlacesTheCaretWhereClicked(bool acceptsReturn, bool caretAtEnd)
    {
        var box = new FuncTextBox { AcceptsReturn = acceptsReturn, Text = "a cat wearing a hat", Width = 300 };
        var other = new TextBox();
        var panel = new StackPanel();
        panel.Children.Add(other);
        panel.Children.Add(box);
        var window = new Window { Content = panel, Width = 400, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        other.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.False(box.IsFocused);

        var point = box.TranslatePoint(new Point(20, box.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(box.IsFocused);
        Assert.Equal(box.SelectionStart, box.SelectionEnd);
        if (caretAtEnd)
        {
            Assert.Equal(box.Text!.Length, box.CaretIndex);
        }
        else
        {
            Assert.InRange(box.CaretIndex, 1, box.Text!.Length - 1);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void LostFocus_Default_DoesNotExecute()
    {
        var (_, other, command) = Create(true);

        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, command.Count);
    }
}
