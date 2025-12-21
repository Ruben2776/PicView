using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using R3;

namespace PicView.Avalonia.History;

public sealed class HistoryManager : IDisposable
{
    private readonly int _capacity = 20;
    private readonly List<HistoryEntry> _collection = new();
    private int _cursor = -1;
    public ObservableCollection<HistoryEntry> Timeline { get; } = new();
    private readonly MainViewModel _vm;
    public HistoryWindowViewModel? WindowVm { get; private set; }
    public HistoryWindow? Window { get; private set; }
    public ReactiveCommand CloseCommand { get; }
    public BindableReactiveProperty<bool> FileChangeHistoryEnabled { get; } = new(Settings.UIProperties.EnableFileChangeHistory);

    // Constructor
    
    public HistoryManager(MainViewModel vm)
    {
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        CloseCommand = new ReactiveCommand(async _ => await Hide());
    }

    public async Task SetHasChanges(bool hasChanges)
    {
        _vm.PicViewer.HasChanges.Value = hasChanges;
        TitleManager.SetTitle(_vm);
    }

    public async Task AddSnapshot(EditKind kind) => await AddSnapshot(kind, null, null);

    public async Task AddSnapshot(EditKind kind, string? description, Bitmap? bitmap)
    {
        if(!FileChangeHistoryEnabled.Value)
        {
            if (kind != EditKind.Open)
                await _vm.HistoryManager.SetHasChanges(true);
            return;
        }

        
        if(kind == EditKind.Open)
            Clear();

        // Treat current viewer image as default
        bitmap ??= _vm.PicViewer.ImageSource.Value as Bitmap;
        if (bitmap is null) return;

        // If user had undone, clear the redo branch
        if (_cursor > 0)
            await Dispatcher.UIThread.InvokeAsync(DiscardRedoBranch);

        // Reindex existing entries
        foreach (var e in _collection) e.Index++;

        if (description is null)
            description = $"{_vm.PicViewer.PixelWidth.Value}×{_vm.PicViewer.PixelHeight.Value} {_vm.PicViewer.FileInfo?.Value?.Length.GetReadableFileSize()}";

        // Insert a lightweight placeholder (spinner on)
        var entry = new HistoryEntry
        {
            Index = 0,
            Kind = kind,
            Description = description,
            IsLoading = true
        };

        // Add to collections and enforce capacity
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _collection.Insert(0, entry);
            Timeline.Insert(0, entry);
            _cursor = 0;
            EnforceCapacity();
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            entry.Snapshot = CloneBitmap(bitmap);
            entry.IsLoading = false;

            // Notify list about updated item
            var i = Timeline.IndexOf(entry);
            if (i >= 0) Timeline[i] = entry;
        });

        if (kind != EditKind.Open)
            await _vm.HistoryManager.SetHasChanges(true);
    }


    // Remove redo items (everything "ahead" of the current cursor)
    private void DiscardRedoBranch()
    {
        // _cursor points at the current state; items 0.._cursor-1 are redo states
        for (int i = 0; i < _cursor; i++)
        {
            var victim = _collection[0];
            _collection.RemoveAt(0);

            var ti = Timeline.IndexOf(victim);
            if (ti >= 0) Timeline.RemoveAt(ti);

            victim.Snapshot?.Dispose();
            victim.Snapshot = null;
        }

        _cursor = 0;

        // Re-index remaining entries to keep UI consistent
        for (int k = 0; k < _collection.Count; k++)
            _collection[k].Index = k;
    }

    // Keep at most _capacity entries; prune from the tail (oldest)
    private void EnforceCapacity()
    {
        while (_collection.Count > _capacity)
        {
            var last = _collection[^1];
            _collection.RemoveAt(_collection.Count - 1);

            var ti = Timeline.IndexOf(last);
            if (ti >= 0) Timeline.RemoveAt(ti);

            last.Snapshot?.Dispose();
            last.Snapshot = null;
        }
    }

    public async Task Undo()
    {
        if (_cursor >= _collection.Count - 1) return;
        _cursor++;
        await RestoreSnapshot(_cursor);
    }

    public async Task Redo()
    {
        if (_cursor <= 0) return;
        _cursor--;
        await RestoreSnapshot(_cursor);
    }

    public async Task JumpTo(int index)
    {
        if (index < 0 || index >= _collection.Count) return;
        _cursor = index;
        await RestoreSnapshot(_cursor);
    }

    private async Task RestoreSnapshot(int index)
    {
        if(!FileChangeHistoryEnabled.Value)
            return;

        if (index < 0 || index >= _collection.Count)
            return;

        var source = _collection[index].Snapshot;
        if (source is null)
            return;

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var cloned = CloneBitmap(source);
            await _vm.ImageViewer.ApplySnapshotBitmap(cloned, _vm);
                   
        }, DispatcherPriority.Render);

        await _vm.HistoryManager.SetHasChanges((_collection[index].Kind != EditKind.Open));
    }



    public void Clear()
    {
        void Core()
        {
             _collection.Clear();
            _cursor = -1;
            Timeline.Clear();
        }

        if (Dispatcher.UIThread.CheckAccess())
            Core();
        else
            Dispatcher.UIThread.Post(Core);
    }


    public async Task Show()
    {
        if (Window is not null)
            return;

        WindowVm ??= new HistoryWindowViewModel(_vm, this);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Window = new HistoryWindow
            {
                Width = 200,
                Height = 320,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(12),
                DataContext = _vm
            };

            UIHelper.GetMainView.MainGrid.Children.Add(Window);
        });        
    }

    public async Task Hide()
    {
        if (Window is null) return;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UIHelper.GetMainView.MainGrid.Children.Remove(Window);
        });
        WindowVm = null;
        Window = null;
    }

    public async Task ToggleHistoryWindow()
    {
        if(!FileChangeHistoryEnabled.Value)
            return;

        if (Window is null)
        {
            await Show();
            _vm.Translation.IsShowingHistoryWindow.Value = TranslationManager.Translation.HideHistoryWindow;
        }
        else
        {
            await Hide();
            _vm.Translation.IsShowingHistoryWindow.Value = TranslationManager.Translation.ShowHistoryWindow;
        }

        MenuManager.CloseMenus(_vm);
    }

    public static Bitmap? CloneBitmap(Bitmap source)
    {
        if(source is null)
            return null;
            
        var size = source.PixelSize;
        var target = new RenderTargetBitmap(size);
        using (var ctx = target.CreateDrawingContext())
        {
            ctx.DrawImage(source,
                new Rect(0, 0, size.Width, size.Height),
                new Rect(0, 0, size.Width, size.Height));
        }
        return target;
    }

    public void Dispose()
    {
        Clear();
    }
}