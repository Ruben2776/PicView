using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using PicView.Core.History;
using PicView.Core.Extensions;
using R3;
using Unit = R3.Unit;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.UI;
using PicView.Core.Models;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Avalonia.Navigation;

namespace PicView.Avalonia.ViewModels;

public sealed class HistoryItemViewModel
{
    public required int Index { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public required Bitmap Thumbnail { get; init; }
    public bool IsActive { get; init; }
    public bool IsFuture { get; init; }
}

public sealed class HistoryOverlayViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
     
    private readonly HistoryManager _history;
    private readonly MainViewModel _vm;

    public ObservableCollection<HistoryItemViewModel> Items { get; } = new();
    public BindableReactiveProperty<string> StatusText { get; } = new();

    public ReactiveCommand UndoCommand { get; }
    public ReactiveCommand RedoCommand { get; }
    public ReactiveCommand<int> RevertToCommand { get; }
    public ReactiveCommand CloseCommand { get; }

    public BindableReactiveProperty<int> SelectedIndex { get; } = new(-1);


    public HistoryOverlayViewModel(MainViewModel vm, HistoryManager history)
    {
        _vm = vm;
        _history = history;

        UndoCommand   = new ReactiveCommand(async _ => await Undo());
        RedoCommand   = new ReactiveCommand(async _ => await Redo());
        RevertToCommand = new ReactiveCommand<int>(async i => await RevertTo(i));
        CloseCommand  = new ReactiveCommand(_ => Hide());

        // reactive sync with history timeline
        _history.TimelineObservable
            .Subscribe(list =>
            {
                Items.Clear();
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    Items.Add(new HistoryItemViewModel
                    {
                        Index = i,
                        Title = e.Kind.ToString(),
                        Subtitle = e.Description,
                        Thumbnail = e.Snapshot.ToThumbnail(128),
                        IsActive = (i == _history.Cursor),
                        IsFuture = (i > _history.Cursor)
                    });
                }


                SelectedIndex.Value = _history.Cursor;

                StatusText.Value = $"Steps: {list.Count}   Current: {_history.Cursor + 1}/{list.Count}";
            })
            .AddTo(_disposables);
    }

    private async ValueTask ApplySnapshot(HistoryEntry entry)
    {
        _vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        var currentFileInfo = _vm.PicViewer.FileInfo.Value;

        var snapshotBitmap = entry.Snapshot.ToAvaloniaBitmap();

        var imageModel = new ImageModel
        {
            Image = snapshotBitmap,
            PixelWidth = snapshotBitmap?.PixelSize.Width ?? 0,
            PixelHeight = snapshotBitmap?.PixelSize.Height ?? 0,
            ImageType = ImageType.Bitmap

        };
        await UpdateImage.SetCroppedImageAsync(imageModel.Image, imageModel.ImageType, String.Concat(currentFileInfo?.Name ?? TranslationManager.Translation.ClipboardImage, "*"), _vm);
        _vm.PicViewer.HasChanges.Value = true;

        _vm.MainWindow.IsLoadingIndicatorShown.Value = false;
    }

    private void UpdateStatusText(string action = "")
    {
        var list = _history.TimelineObservable.Value;
        var current = _history.Cursor + 1;
        var total = list.Count;

        var prefix = string.IsNullOrEmpty(action) ? "" : $"{action}: ";
        string desc = _history.Current?.Description ?? _history.Current?.Kind.ToString() ?? "";
        StatusText.Value = $"{prefix}Step {current} of {total} — {desc}";
    }


    private async Task Undo()
    {
        var e = _history.Undo();
        if (e is null) return;
        await ApplySnapshot(e);
        SelectedIndex.Value = _history.Cursor;
        UpdateStatusText("Undo");
    }

    private async Task Redo()
    {
        var e = _history.Redo();
        if (e is null) return;
        await ApplySnapshot(e);
        SelectedIndex.Value = _history.Cursor;
        UpdateStatusText("Redo");
    }

    private async Task RevertTo(int index)
    {
        var e = _history.JumpTo(index);
        if (e is null) return;
        await ApplySnapshot(e);
        SelectedIndex.Value = index;
        UpdateStatusText("Reverted to");
    }

    // public void Show() => OverlayHost.ShowHistory();
    public void Hide() => OverlayHost.HideHistory();

    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
