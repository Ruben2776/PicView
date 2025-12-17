using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using R3;

namespace PicView.Avalonia.UI;

public sealed class DebouncedLoadingScope : IAsyncDisposable
{
    private readonly BindableReactiveProperty<bool> _target;
    private readonly int _delayMs;
    private readonly CancellationTokenSource _cts = new();
    private bool _shown;

    private DebouncedLoadingScope(BindableReactiveProperty<bool> target, int delayMs)
    {
        _target = target;
        _delayMs = delayMs;

        // fire-and-forget debounce task
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delayMs, _cts.Token);
                if (_cts.IsCancellationRequested)
                    return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _target.Value = true;
                    _shown = true;
                });
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        });
    }

    public static DebouncedLoadingScope Start(BindableReactiveProperty<bool> target) => new(target, 150);
    public static DebouncedLoadingScope Start(BindableReactiveProperty<bool> target, int delayMs) => new(target, delayMs);

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_shown)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _target.Value = false;
            });
        }

        _cts.Dispose();
    }
}
