using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Tests.UI;

/// <summary>
/// Messages handed to a Main Window's view model, which the window shows.
/// </summary>
[Collection("Sequential")]
public class TooltipMessagesTests
{
    public TooltipMessagesTests() => SetDefaults();

    [AvaloniaFact]
    public async Task FollowMessages_FromAPoolThread_ShowsOnTheUiThreadWithItsSettings()
    {
        var vm = new MainWindowViewModel(null!, null!, null!, null!) { ToolTip = new ToolTipViewModel() };
        var shown = new List<(string Message, bool Center, TimeSpan Interval, bool OnUiThread)>();
        using var following = TooltipHelper.FollowMessages(vm.ToolTip, (message, center, interval) =>
        {
            shown.Add((message, center, interval, Dispatcher.UIThread.CheckAccess()));
            return Task.CompletedTask;
        });

        await Task.Run(() => TooltipHelper.ShowTooltipMessage(vm, "taken", true, TimeSpan.FromSeconds(3)),
            TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();

        var show = Assert.Single(shown);
        Assert.Equal(("taken", true, TimeSpan.FromSeconds(3), true), show);
    }

    [AvaloniaFact]
    public void FollowMessages_SameMessageTwice_ShowsItTwice()
    {
        var vm = new MainWindowViewModel(null!, null!, null!, null!) { ToolTip = new ToolTipViewModel() };
        var shown = new List<string>();
        using var following = TooltipHelper.FollowMessages(vm.ToolTip, (message, _, _) =>
        {
            shown.Add(message);
            return Task.CompletedTask;
        });

        TooltipHelper.ShowTooltipMessage(vm, "taken", true);
        TooltipHelper.ShowTooltipMessage(vm, "taken", true);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["taken", "taken"], shown);
        Assert.Null(vm.ToolTip.ToolTipMessageSource.CurrentValue);
    }

    [AvaloniaFact]
    public void FollowMessages_Disposed_ShowsNoMore()
    {
        var vm = new MainWindowViewModel(null!, null!, null!, null!) { ToolTip = new ToolTipViewModel() };
        var shown = 0;
        var following = TooltipHelper.FollowMessages(vm.ToolTip, (_, _, _) =>
        {
            shown++;
            return Task.CompletedTask;
        });

        following.Dispose();
        TooltipHelper.ShowTooltipMessage(vm, "taken");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, shown);
    }
}
