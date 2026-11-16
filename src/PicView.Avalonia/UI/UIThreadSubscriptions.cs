using Avalonia.Threading;
using R3;

namespace PicView.Avalonia.UI;

/// <summary>
/// Subscriptions that update controls, for observables that may emit on another thread, such as a tab's comparison,
/// which changes on the thread that shows the tab's next pair.
/// </summary>
internal static class UIThreadSubscriptions
{
    /// <summary>
    /// Runs <paramref name="show"/> on the UI thread for every value: at once when the value comes on the UI thread,
    /// else posted. <paramref name="show"/> reads the state it shows when it runs, so a posted run shows the latest
    /// state rather than the one it was posted for.
    /// </summary>
    public static IDisposable SubscribeOnUIThread<T>(this Observable<T> source, Action show,
        Action<Result> onCompleted) =>
        source.Subscribe(_ => RunOnUIThread(show), onCompleted);

    private static void RunOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action, DispatcherPriority.Send);
    }
}
