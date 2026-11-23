using System.Windows.Input;
using Avalonia.Controls;
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

    [AvaloniaFact]
    public void LostFocus_Default_DoesNotExecute()
    {
        var (_, other, command) = Create(true);

        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, command.Count);
    }
}
