using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.History;
using R3;

namespace PicView.Avalonia.ViewModels;

public sealed class HistoryItemViewModel
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public BindableReactiveProperty<bool> IsLoading { get; set; } = new(true);
    public BindableReactiveProperty<bool> IsActive { get; set; } = new(false);
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

        // populate immediately so history shows even if window opens late
        Dispatcher.UIThread.Post(UpdateTimeline);

        // Restore snapshot when user selects a history row
        SelectedIndex
            .Subscribe(async i =>
            {
                if (_selectionChangedGuard) return;
                if (i < 0 || i >= _history.Timeline.Count) return;

                // Use HistoryManager to produce/apply the snapshot via the viewer pipeline
                var bmp = await _history.JumpTo(i);
                if (bmp is null) return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                    _vm.ImageViewer.ApplyBitmapAndRefresh(bmp, _vm));
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
                Title = entry.Kind.ToString(),
                Subtitle = entry.Description
            };
            itemVm.IsLoading.Value = entry.IsLoading;
            itemVm.IsActive.Value  = entry.Index == 0;
            Items.Add(itemVm);
        }

        // Choose the active entry (Index == 0) if present
        var activeIdxInList = _history.Timeline
            .Select((e, pos) => new { e, pos })
            .FirstOrDefault(x => x.e.Index == 0)?.pos ?? -1;

        _selectionChangedGuard = true;             // ← prevent Restore on init
        if (activeIdxInList >= 0)
            SelectedIndex.Value = activeIdxInList;
        else if (Items.Count > 0 && SelectedIndex.Value < 0)
            SelectedIndex.Value = 0;
        _selectionChangedGuard = false;            // ← re-enable for user changes
    }


    public void Dispose()
    {
        _disposables.Dispose();
        _history.Timeline.CollectionChanged -= OnTimelineChanged;
    }
}



// public sealed class HistoryWindowViewModel : IDisposable
// {
//     private readonly MainViewModel _vm;
//     private readonly HistoryManager _history;
//     private readonly CompositeDisposable _disposables = new();

//     public ObservableCollection<HistoryItemViewModel> Items { get; } = new();
//     public BindableReactiveProperty<int> SelectedIndex { get; } = new(-1);

//     public HistoryWindowViewModel(MainViewModel vm, HistoryManager history)
//     {
//         _vm = vm;
//         _history = history;

//         _history.Timeline.CollectionChanged += (_, __) =>
//             Dispatcher.UIThread.Post(UpdateTimeline);

//         SelectedIndex
//             .Subscribe(async i =>
//             {
//                 if (i < 0 || i >= _history.Timeline.Count) return;
//                 var bmp = await _history.JumpTo(i);
//                 if (bmp is null) return;
//                 _vm.PicViewer.ImageSource.Value = bmp;
//                 _vm.PicViewer.HasChanges.Value = true;
//             })
//             .AddTo(_disposables);
//     }

//     private void UpdateTimeline()
//     {
//         if (_history.Timeline.Count < Items.Count)
//             Items.Clear();
        
//         foreach (var entry in _history.Timeline)
//         {
//             var existing = Items.FirstOrDefault(x => x.Index == entry.Index);
//             if (existing is null)
//             {
//                 var itemVm = new HistoryItemViewModel
//                 {
//                     Index = entry.Index,
//                     Title = entry.Kind.ToString(),
//                     Subtitle = entry.Description
//                 };

//                 itemVm.IsLoading.Value = entry.IsLoading;
//                 itemVm.IsActive.Value = entry.Index == 0;

//                 Items.Add(itemVm);
//             }
//             else
//             {
//                 existing.IsLoading.Value = entry.IsLoading;
//                 existing.IsActive.Value = entry.Index == 0;
//             }
//         }

//         SelectedIndex.Value = 0;
//     }


//     public void Dispose()
//     {
//         _disposables.Dispose();
//     }
// }