using System.Reflection;
using System.Windows.Input;
using PicView.Core.IPlatform;
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
        var mapper = DispatchProxy.Create<IFunctionsMapper, KeybindRouterTests.RecordingMapper>();
        _calls = ((KeybindRouterTests.RecordingMapper)(object)mapper).Calls;
        _vm.Mapper = mapper;
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
    public void Execute_WithoutMapper_DoesNothing()
    {
        _vm.Mapper = null;

        ((ICommand)_vm.RunActionCommand).Execute(Actions.Next);

        Assert.Empty(_calls);
    }
}
