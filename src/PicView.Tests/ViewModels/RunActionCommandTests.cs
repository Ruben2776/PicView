using System.Windows.Input;
using PicView.Core.Keybindings;
using PicView.Core.ViewModels;
using PicView.Tests.Input;

namespace PicView.Tests.ViewModels;

[Collection("Sequential")]
public class RunActionCommandTests
{
    private readonly MainWindowViewModel _vm;
    private readonly List<string> _calls;

    public RunActionCommandTests()
    {
        SetDefaults();
        _vm = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var actionRunner = new RecordingActionRunner();
        _calls = actionRunner.Calls;
        _vm.ActionRunner = actionRunner;
    }

    [Fact]
    public void Execute_ActionParameter_RunsThatAction()
    {
        // A button passes its CommandParameter as an object, as axaml does
        ICommand command = _vm.RunActionCommand;

        Assert.True(command.CanExecute(Actions.ZoomIn));
        command.Execute(Actions.ZoomIn);

        Assert.Equal([Actions.ZoomIn], _calls);
    }

    [Fact]
    public void Execute_WithoutActionRunner_DoesNothing()
    {
        _vm.ActionRunner = null;

        ((ICommand)_vm.RunActionCommand).Execute(Actions.Next);

        Assert.Empty(_calls);
    }
}
