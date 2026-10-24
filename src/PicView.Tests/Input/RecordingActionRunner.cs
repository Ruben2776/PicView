using PicView.Core.IPlatform;

namespace PicView.Tests.Input;

/// <summary>
/// Records every Action run by its name, and every other operation by the method's name.
/// </summary>
public sealed class RecordingActionRunner : IActionRunner
{
    public List<string> Calls { get; } = [];

    public ValueTask Run(string action) => Record(action);

    public ValueTask OpenPathAsync(string path) => Record(nameof(OpenPathAsync));

    public ValueTask StopRepeatedNavigation() => Record(nameof(StopRepeatedNavigation));

    public ValueTask ShowStartUpMenu() => Record(nameof(ShowStartUpMenu));

    private ValueTask Record(string call)
    {
        Calls.Add(call);
        return ValueTask.CompletedTask;
    }
}
