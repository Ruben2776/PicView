using Avalonia.Threading;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// Scrolls and zooms the image viewer of the active tab for the Actions of the same name.
/// </summary>
/// <remarks>The viewer is UI-bound, but an Action can arrive on any thread, so these run on the UI thread, inline when
/// they are already on it.</remarks>
public static class ViewerActions
{
    public static ValueTask ScrollLine(MainWindowViewModel vm, bool down) =>
        OnUiThread(vm, down ? ScrollLineDown : ScrollLineUp);

    public static ValueTask ScrollToTop(MainWindowViewModel vm) => OnUiThread(vm, ScrollToTopOf);

    public static ValueTask ScrollToBottom(MainWindowViewModel vm) => OnUiThread(vm, ScrollToBottomOf);

    public static ValueTask ZoomIn(MainWindowViewModel vm) => OnUiThread(vm, ZoomInOf);

    public static ValueTask ZoomOut(MainWindowViewModel vm) => OnUiThread(vm, ZoomOutOf);

    internal static void ScrollLine(IZoomScrollTarget? target, bool down) => target?.ScrollLine(down);

    internal static void ScrollToTop(IZoomScrollTarget? target) => target?.ScrollToHome();

    internal static void ScrollToBottom(IZoomScrollTarget? target) => target?.ScrollToEnd();

    internal static void ZoomIn(IZoomScrollTarget? target) => target?.ZoomIn();

    internal static void ZoomOut(IZoomScrollTarget? target) => target?.ZoomOut();

    // Cached once, so running an Action on the UI thread allocates nothing
    private static readonly Action<IZoomScrollTarget> ScrollLineDown = target => ScrollLine(target, true);
    private static readonly Action<IZoomScrollTarget> ScrollLineUp = target => ScrollLine(target, false);
    private static readonly Action<IZoomScrollTarget> ScrollToTopOf = target => ScrollToTop(target);
    private static readonly Action<IZoomScrollTarget> ScrollToBottomOf = target => ScrollToBottom(target);
    private static readonly Action<IZoomScrollTarget> ZoomInOf = target => ZoomIn(target);
    private static readonly Action<IZoomScrollTarget> ZoomOutOf = target => ZoomOut(target);

    private static ValueTask OnUiThread(MainWindowViewModel vm, Action<IZoomScrollTarget> action)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not IZoomScrollTarget target)
        {
            return ValueTask.CompletedTask;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            action(target);
            return ValueTask.CompletedTask;
        }

        return new ValueTask(Dispatcher.UIThread.InvokeAsync(() => action(target)).GetTask());
    }
}
