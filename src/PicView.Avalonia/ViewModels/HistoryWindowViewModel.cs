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
    public BindableReactiveProperty<Bitmap?> Thumbnail { get; set; } = new(null);
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

    public HistoryWindowViewModel(MainViewModel vm, HistoryManager history)
    {
        _vm = vm;
        _history = history;

        _history.Timeline.CollectionChanged += (_, __) =>
            Dispatcher.UIThread.Post(UpdateTimeline);

        SelectedIndex
            .Subscribe(async i =>
            {
                if (i < 0 || i >= _history.Timeline.Count) return;
                var bmp = await _history.JumpTo(i);
                if (bmp is null) return;
                _vm.PicViewer.ImageSource.Value = bmp;
                _vm.PicViewer.HasChanges.Value = true;
            })
            .AddTo(_disposables);
    }

    private void UpdateTimeline()
    {
        if (_history.Timeline.Count < Items.Count)
            Items.Clear();
        
        foreach (var entry in _history.Timeline)
        {
            var existing = Items.FirstOrDefault(x => x.Index == entry.Index);
            if (existing is null)
            {
                var itemVm = new HistoryItemViewModel
                {
                    Index = entry.Index,
                    Title = entry.Kind.ToString(),
                    Subtitle = entry.Description
                };

                itemVm.Thumbnail.Value = entry.CachedThumbnail;
                itemVm.IsLoading.Value = entry.IsLoading;
                itemVm.IsActive.Value = entry.Index == 0;

                Items.Add(itemVm);
            }
            else
            {
                existing.Thumbnail.Value = entry.CachedThumbnail;
                existing.IsLoading.Value = entry.IsLoading;
                existing.IsActive.Value = entry.Index == 0;
            }
        }

        SelectedIndex.Value = 0;
    }


    public void Dispose()
    {
        foreach (var item in Items)
            item.Thumbnail?.Dispose();

        _disposables.Dispose();
    }
}