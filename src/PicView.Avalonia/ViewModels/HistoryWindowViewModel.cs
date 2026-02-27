using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.History;
using R3;

namespace PicView.Avalonia.ViewModels;

public sealed class HistoryItemViewModel
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public EditKind Kind { get; set; }
    public BindableReactiveProperty<bool> IsLoading { get; set; } = new(true);
    public BindableReactiveProperty<bool> IsActive { get; set; } = new(false);
    public BindableReactiveProperty<bool> IsRedoBranch { get; set; } = new(false);
}

public sealed class HistoryWindowViewModel : IDisposable
{
    private readonly MainViewModel _vm;
    private readonly HistoryManager _history;
    private readonly CompositeDisposable _disposables = new();
    public ObservableCollection<HistoryItemViewModel> Items { get; } = new();
    public BindableReactiveProperty<int> SelectedIndex { get; } = new(-1);
    private bool _selectionChangedGuard;


    public HistoryWindowViewModel(MainViewModel vm, HistoryManager history)
    {
        _vm = vm;
        _history = history;

        _history.Timeline.CollectionChanged += OnTimelineChanged;

        // Populate immediately so history shows even if window opens late
        Dispatcher.UIThread.Post(UpdateTimeline);

        // Restore snapshot when user selects a history row
        SelectedIndex
            .Subscribe(async i =>
            {
                if (_selectionChangedGuard) return;
                if (i < 0 || i >= _history.Timeline.Count) return;

                await _history.JumpTo(i);

                // Recompute redo-branch after restore
                var cursor = _history.Timeline[i].Index;
                foreach (var it in Items)
                    it.IsRedoBranch.Value = it.Index < cursor;
            })
            .AddTo(_disposables);
    }

    private void OnTimelineChanged(object? sender, EventArgs e)
        => Dispatcher.UIThread.Post(UpdateTimeline);

    private void UpdateTimeline()
    {
        Items.Clear();

        foreach (var entry in _history.Timeline)
        {
            var itemVm = new HistoryItemViewModel
            {
                Index = entry.Index,
                Title = entry.Kind.GetDisplayName(),
                Subtitle = entry.Description,
                Kind = entry.Kind
            };
            itemVm.IsLoading.Value = entry.IsLoading;
            itemVm.IsActive.Value  = entry.Index == 0;
            Items.Add(itemVm);
        }

        // Choose the active entry (Index == 0) if present
        var activeIdxInList = _history.Timeline
            .Select((e, pos) => new { e, pos })
            .FirstOrDefault(x => x.e.Index == 0)?.pos ?? -1;

        _selectionChangedGuard = true;

        if (activeIdxInList >= 0)
            SelectedIndex.Value = activeIdxInList;
        else if (Items.Count > 0 && SelectedIndex.Value < 0)
            SelectedIndex.Value = 0;

        _selectionChangedGuard = false;

        // Compute redo branch (entries "newer" than currently active)
        // Newest has Index==0. If user undoes to entry with Index=N,
        // items with Index < N are redo-able and should be greyed.
        var cursor = (_history.Timeline.Count > 0) ? _history.Timeline[SelectedIndex.Value].Index : 0;
        foreach (var it in Items)
            it.IsRedoBranch.Value = it.Index < cursor;
    }


    public void Dispose()
    {
        _disposables.Dispose();
        _history.Timeline.CollectionChanged -= OnTimelineChanged;
    }
}