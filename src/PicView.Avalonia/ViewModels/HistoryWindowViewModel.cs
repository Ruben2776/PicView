using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using PicView.Core.Extensions;
using R3;
using R3.Avalonia;
using Unit = R3.Unit;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.UI;
using PicView.Core.Models;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Avalonia.Navigation;
using ImageMagick;
using PicView.Avalonia.History;
using Avalonia.Threading;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.ViewModels
{
    public sealed class HistoryItemViewModel : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public int Index { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }

        public BindableReactiveProperty<Bitmap?> Thumbnail { get; set; } = new();

        public BindableReactiveProperty<bool> IsActive { get; set; } = new();
        public bool IsFuture { get; set; }

        public void Dispose() => _disposables.Dispose();
    }

    public sealed class HistoryWindowViewModel : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        private const int ThumbnailSize = 100;

        private readonly HistoryManager _history;
        private readonly MainViewModel _vm;

        public ObservableCollection<HistoryItemViewModel> Items { get; } = new();
        public BindableReactiveProperty<string> StatusText { get; } = new();
        public ReactiveCommand CloseCommand { get; }

        public BindableReactiveProperty<int> SelectedIndex { get; } = new(-1);

        public HistoryWindowViewModel(MainViewModel vm, HistoryManager history)
        {
            _vm = vm;
            _history = history;

            CloseCommand = new ReactiveCommand(_ => Hide());

            _history.TimelineObservable
                .AsObservable() // or .ToObservable() or just use the source itself
                .Subscribe(list => Dispatcher.UIThread.Post(() => UpdateTimeline(list)))
                .AddTo(_disposables);

            SelectedIndex.Subscribe(async i =>
            {
                if (_suppressSelection)
                    return;
                if (i < 0 || i >= _history.Count())
                    return;

                var frame = _history.JumpTo(i);
                if (frame is null)
                    return;

                await ApplySnapshot(frame);
            }).AddTo(_disposables);

            
        }


        private bool _suppressSelection;

        private void UpdateTimeline(IReadOnlyList<HistoryEntry> list)
        {
            _suppressSelection = true;
            try
            {
                // Trim extra items if the new list is shorter
                while (Items.Count > list.Count)
                {
                    var last = Items[^1];
                    if (last.Thumbnail.Value is Bitmap bmp)
                        bmp.Dispose();

                    Items.RemoveAt(Items.Count - 1);
                }

                // Upsert items efficiently
                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];

                    if (i >= Items.Count)
                    {
                        // Add new view-model item
                        Items.Add(new HistoryItemViewModel
                        {
                            Index = entry.Index,
                            Title = entry.Kind.ToString(),
                            Subtitle = entry.Description,
                            Thumbnail = new BindableReactiveProperty<Bitmap?>(entry.CachedThumbnail as Bitmap),
                            IsActive = new BindableReactiveProperty<bool>(i == _history.Cursor)
                        });
                    }
                    else
                    {
                        // Update existing item only if something changed
                        var vmItem = Items[i];

                        if (vmItem.Index != entry.Index)
                            vmItem.Index = entry.Index;

                        var newTitle = entry.Kind.ToString();
                        if (vmItem.Title != newTitle)
                            vmItem.Title = newTitle;

                        if (vmItem.Subtitle != entry.Description)
                            vmItem.Subtitle = entry.Description;

                        var shouldBeActive = (i == _history.Cursor);
                        if (vmItem.IsActive.Value != shouldBeActive)
                            vmItem.IsActive.Value = shouldBeActive;

                        // Handle thumbnail replacement safely
                        if (entry.CachedThumbnail is Bitmap newThumb)
                        {
                            if (!ReferenceEquals(vmItem.Thumbnail.Value, newThumb))
                            {
                                if (vmItem.Thumbnail.Value is Bitmap oldThumb)
                                    oldThumb.Dispose();

                                vmItem.Thumbnail.Value = newThumb;
                            }
                        }
                    }
                }

                // Keep selection in sync with cursor
                if (SelectedIndex.Value != _history.Cursor)
                    SelectedIndex.Value = _history.Cursor;
            }
            finally
            {
                _suppressSelection = false;
            }
        }


        /// <summary>
        /// Convert MagickImage to Avalonia Bitmap off the UI thread, then apply minimal UI updates.
        /// </summary>
        private async ValueTask ApplySnapshot(MagickImage frame)
        {
            _vm.MainWindow.IsLoadingIndicatorShown.Value = true;

            try
            {
                // 1) Heavy conversion OFF the UI thread
                var bitmap = await Task.Run(() => frame.ToAvaloniaBitmap()).ConfigureAwait(false);

                // 2) Minimal UI-thread updates
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Replace full-size bitmap safely (dispose previous)
                    var oldBmp = (Bitmap?)_vm.PicViewer.CachedImage.Value;
                    _vm.PicViewer.CachedImage.Value = bitmap;
                    _vm.PicViewer.ImageSource.Value = bitmap;
                    oldBmp?.Dispose();

                    // Set current frame for the viewer (no extra cloning)
                    _vm.PicViewer.MagickFrame.Value = frame;

                    // Mark as changed so Save/Export flows know there’s a delta
                    _vm.PicViewer.HasChanges.Value = true;

                    // Keep selection in sync with the history cursor
                    if (SelectedIndex.Value != _history.Cursor)
                        SelectedIndex.Value = _history.Cursor;
                });
            }
            finally
            {
                _vm.MainWindow.IsLoadingIndicatorShown.Value = false;
            }
        }

        public void Hide()
        {
            HistoryWindowHost.HideHistory();
            _vm.MainWindow.IsHistoryWindowShown.Value = false;
        }

        public void Dispose()
        {
            // dispose thumbnails we own
            foreach (var it in Items)
                it.Thumbnail.Value?.Dispose();

            _disposables.Dispose();
        }
    }
}
