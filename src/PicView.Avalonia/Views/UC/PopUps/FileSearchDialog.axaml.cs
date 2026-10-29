using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Core.DebugTools;
using PicView.Core.FileSearch;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class FileSearchDialog : AnimatedPopUp
{
    private DisposableBag _disposables;

    /// <summary>
    /// The files matching the search, in the active tab of this dialog's Main Window.
    /// </summary>
    public BindableReactiveProperty<ObservableCollection<FileSearchResult>?> FilteredFileInfos { get; } = new();

    /// <summary>
    /// Opens the picked file in this dialog's Main Window.
    /// </summary>
    public ReactiveCommand<string> LoadFromStringCommand { get; }

    public FileSearchDialog()
    {
        LoadFromStringCommand = new ReactiveCommand<string>(LoadSelectedFileAsync);
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        DataContext = core;
        InitializeComponent();
        if (OperatingSystem.IsMacOS())
        {
            // There's a bug on mac where the first key press is incorrectly captured, e.g., pressing
            // Cmd+F will input f to the search, so it should be readonly to prevent text from being inserted
            SearchBox.IsReadOnly = true;
        }
        Loaded += OnLoaded;
    }

    internal async ValueTask LoadSelectedFileAsync(string source, CancellationToken ct)
    {
        // Through SourceOpener, so a tab showing a single image gets a new iterator and its docked gallery back
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow || DataContext is not CoreViewModel core)
        {
            return;
        }

        await SourceOpener.OpenAsync(mainWindow, core, source).ConfigureAwait(false);
    }

    /// <summary>
    /// The tabs of the Main Window this dialog is shown in. Read on the UI thread.
    /// </summary>
    private TabOverviewViewModel? OwnTabs => (TopLevel.GetTopLevel(this) as MainWindow)?.ViewModel?.WindowTabs;

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        SetupSearchSubscription();
        SearchBox.Focus();
        AddHandler(KeyDownEvent, KeysDownAsync, RoutingStrategies.Tunnel);

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        LoadFromStringCommand.Subscribe(_ => CloseMenu(sender, e)).AddTo(ref _disposables);
        
        if (OperatingSystem.IsMacOS())
        {
            Dispatcher.Post(() =>
            {
                // Allow text after we are sure the first keystroke is not captured
                SearchBox.IsReadOnly = false;
            }, DispatcherPriority.Background);
        }
    }

    private async ValueTask KeysDownAsync(object? sender, KeyEventArgs e)
    {
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

                    await LoadSelectedFileAsync(SearchBox.Text, CancellationToken.None).ConfigureAwait(false);
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
        // Captured on the UI thread, since the search below runs off it
        if (OwnTabs is not { } tabs)
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
                        FilteredFileInfos.Value?.Clear();
                        return;
                    }

                    const int batchSize = 25;

                    // A tab showing a single image has no files to search
                    if (tabs.ActiveTab.CurrentValue.ImageIterator is not { } iterator)
                    {
                        FilteredFileInfos.Value?.Clear();
                        return;
                    }

                    IEnumerable<FileSearchResult>? results = null;
                    await Task.Run(
                        () => { results = FileSearcher.GetFileSearchResults(iterator.Files, text); }, ct).ConfigureAwait(false);

                    var fileSearchResults = results as FileSearchResult[] ?? results.ToArray();
                    FilteredFileInfos.Value =
                        new ObservableCollection<FileSearchResult>(fileSearchResults.Take(batchSize));
                    if (fileSearchResults.Length < batchSize)
                    {
                        return;
                    }

                    // Need to delay to make it feel smooth
                    await Task.Delay(10, ct).ConfigureAwait(false);

                    for (var i = batchSize; i < fileSearchResults.Length; i += batchSize)
                    {
                        var batch = fileSearchResults.Skip(i).Take(batchSize);
                        foreach (var item in batch)
                        {
                            await Task.Delay(10, ct).ConfigureAwait(false);
                            ct.ThrowIfCancellationRequested();
                            if (!ct.IsCancellationRequested)
                            {
                                FilteredFileInfos.Value.Add(item);
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

        FilteredFileInfos.CurrentValue?.Clear();
    }

    private void CloseMenu(object? sender, RoutedEventArgs e)
    {
        _ = AnimatedClosing();
    }
}