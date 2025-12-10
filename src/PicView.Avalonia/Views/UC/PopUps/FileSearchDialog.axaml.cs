using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileSearch;
using R3;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class FileSearchDialog : AnimatedPopUp
{
    private readonly CompositeDisposable _disposables = new();

    public FileSearchDialog()
    {
        DataContext = UIHelper.GetMainView.DataContext as MainViewModel;
        if (DataContext is MainViewModel { PicViewer.FilteredFileInfos.CurrentValue: null } vm)
        {
            vm.PicViewer.FilteredFileInfos.Value = [];
        }

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Ensure we don't double-subscribe if Loaded fires multiple times
        _disposables.Clear();
        SetupSearchSubscription();

        SearchBox.Focus();

        AddHandler(KeyDownEvent, KeysDownAsync, RoutingStrategies.Tunnel);
    }

    private async ValueTask KeysDownAsync(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                MoveFocus(NavigationDirection.Next);
                e.Handled = true;
                break;
            case Key.Up:
                MoveFocus(NavigationDirection.Previous);
                e.Handled = true;
                break;
            case Key.Enter:
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    return;
                }

                if (uint.TryParse(SearchBox.Text, out var result))
                {
                    e.Handled = true;
                    var desiredIndex = result <= 0 ? 0 : Math.Min(NavigationManager.GetCount - 1, result - 1);
                    await ImageLoader.CheckCancellationAndStartIterateToIndex((int)desiredIndex,
                            NavigationManager.ImageIterator, CancellationToken.None)
                        .ConfigureAwait(false);
                }

                break;
        }
    }

    private void MoveFocus(NavigationDirection direction)
    {
        if (TopLevel.GetTopLevel(this) is not { FocusManager: { } focusManager })
        {
            return;
        }

        var focused = focusManager.GetFocusedElement();
        if (focused is null)
        {
            return;
        }

        var next = KeyboardNavigationHandler.GetNext(focused, direction);
        next?.Focus();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        Dispose();
    }

    private void SetupSearchSubscription()
    {
        if (DataContext is not MainViewModel vm)
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
                    vm.PicViewer.FilteredFileInfos.Value?.Clear();
                    return;
                }

                const int batchSize = 25;

                IEnumerable<FileSearchResult>? results = null;
                await Task.Run(
                    () => { results = FileSearcher.GetFileSearchResults(NavigationManager.GetCollection, text); }, ct);

                var fileSearchResults = results as FileSearchResult[] ?? results.ToArray();
                vm.PicViewer.FilteredFileInfos.Value =
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
                            vm.PicViewer.FilteredFileInfos.Value.Add(item);
                        }
                    }
                }
            }, AwaitOperation.Switch, false)
            // Add the subscription to our disposable manager for cleanup.
            .AddTo(_disposables);

        // Close when changing picture
        Observable.EveryValueChanged(vm.PicViewer, x => x.FileInfo.CurrentValue)
            .Skip(1)
            .SubscribeAwait(async (_, _) => { await AnimatedClosing(); })
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
        RemoveHandler(KeyDownEvent, KeysDownAsync);
        
        if (DataContext is MainViewModel vm)
        {
            vm.PicViewer.FilteredFileInfos.Value.Clear();
        }
    }
}