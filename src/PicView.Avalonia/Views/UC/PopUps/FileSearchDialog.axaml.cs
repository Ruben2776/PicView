using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.FileSearch;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class FileSearchDialog : AnimatedPopUp
{
    private DisposableBag _disposables;

    public FileSearchDialog()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        DataContext = core;
        core.SharedNavigationService.FilteredFileInfos ??= new BindableReactiveProperty<ObservableCollection<FileSearchResult>?>();
        core.SharedNavigationService.LoadFromStringCommand ??= new ReactiveCommand<string>(LoadSelectedFile);

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private static async ValueTask LoadSelectedFile(string source, CancellationToken ct)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        await core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.LoadFromStringAsync(source);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SetupSearchSubscription();
        SearchBox.Focus();        
        AddHandler(KeyDownEvent, KeysDownAsync, RoutingStrategies.Tunnel);
        
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        core.SharedNavigationService.LoadFromStringCommand.Subscribe(_ => CloseMenu(sender, e)).AddTo(ref _disposables);
    }

    private async ValueTask KeysDownAsync(object? sender, KeyEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        switch (e.Key)
        {
            case Key.Down:
                MoveFocus(NavigationDirection.Down);
                e.Handled = true;
                break;
            case Key.Up:
                MoveFocus(NavigationDirection.Up);
                e.Handled = true;
                break;
            case Key.Enter:
                if (SearchBox.IsFocused)
                {
                    if (string.IsNullOrWhiteSpace(SearchBox.Text))
                    {
                        return;
                    }
                
                    var tabs = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs;
                    await tabs.LoadFromFileAsync(SearchBox.Text).ConfigureAwait(false);
                }
                else
                {
                    if (TopLevel.GetTopLevel(this) is not { FocusManager: { } focusManager })
                    {
                        return;
                    }

                    var focused = focusManager.GetFocusedElement();
                    if (focused is not Button button)
                    {
                        return;
                    }
                    button.Command.Execute(button.CommandParameter);
                }
                e.Handled = true;
                break;
        }
    }

    private void MoveFocus(NavigationDirection direction)
    {
        if (TopLevel.GetTopLevel(this) is not { FocusManager: { } focusManager })
        {
            return;
        }
        focusManager.TryMoveFocus(direction);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        Dispose();
    }

    private void SetupSearchSubscription()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        // Create an observable that emits a value whenever SearchQuery changes.
        Observable.EveryValueChanged(SearchBox, x => x.Text)
            .Skip(1)
            // Wait for X ms of inactivity before processing to avoid searching on every keystroke.
            .Debounce(TimeSpan.FromMilliseconds(50))
            // Subscribe to the results and update the UI collection.
            .SubscribeAwait(async (text, ct) =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    core.SharedNavigationService.FilteredFileInfos.Value?.Clear();
                    return;
                }

                const int batchSize = 25;

                var tabs = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs;
                var tab = tabs.ActiveTab.CurrentValue;
                IEnumerable<FileSearchResult>? results = null;
                await Task.Run(
                    () => { results = FileSearcher.GetFileSearchResults(tab.ImageIterator.Files, text); }, ct);

                var fileSearchResults = results as FileSearchResult[] ?? results.ToArray();
                core.SharedNavigationService.FilteredFileInfos.Value = 
                    new ObservableCollection<FileSearchResult>(fileSearchResults.Take(batchSize));
                if (fileSearchResults.Length < batchSize)
                {
                    return;
                }

                // Need to delay to make it feel smooth
                await Task.Delay(10, ct);

                for (var i = batchSize; i < fileSearchResults.Length; i += batchSize)
                {
                    var batch = fileSearchResults.Skip(i).Take(batchSize);
                    foreach (var item in batch)
                    {
                        await Task.Delay(10, ct);
                        ct.ThrowIfCancellationRequested();
                        if (!ct.IsCancellationRequested)
                        {
                            core.SharedNavigationService.FilteredFileInfos.Value.Add(item);
                        }
                    }
                }
            }, DebugHelper.LogError(nameof(FileSearchDialog), nameof(SetupSearchSubscription)),
                AwaitOperation.Switch, false)
            .AddTo(ref _disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
        RemoveHandler(KeyDownEvent, KeysDownAsync);
        
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        core.SharedNavigationService?.FilteredFileInfos?.CurrentValue?.Clear();
    }

    private void CloseMenu(object? sender, RoutedEventArgs e)
    {
        _ = AnimatedClosing();
    }
}