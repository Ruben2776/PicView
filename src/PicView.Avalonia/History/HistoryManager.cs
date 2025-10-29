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
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using R3;

namespace PicView.Avalonia.History;

public sealed class HistoryManager : IDisposable
{
    private readonly int _capacity;
    private readonly List<HistoryEntry> _collection = new();
    private int _cursor = -1;
    public ObservableCollection<HistoryEntry> Timeline { get; } = new();
    private readonly MainViewModel _vm;
    public HistoryWindowViewModel? WindowVm { get; private set; }
    public HistoryWindow? Window { get; private set; }
    public ReactiveCommand CloseCommand { get; }

    // Constructor
    public HistoryManager(MainViewModel vm, int capacity = 20)
    {
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        _capacity = Math.Max(2, capacity);

        CloseCommand = new ReactiveCommand(async _ => await Hide());
    }

    public async Task AddSnapshot(EditKind kind, string description, Bitmap? bitmap = null)
    {
        if(kind == EditKind.Open)
            Clear();

        // Treat current viewer image as default
        bitmap ??= _vm.PicViewer.ImageSource.Value as Bitmap;
        if (bitmap is null) return;

        // Standard behavior: if user had undone, clear the redo branch
        if (_cursor > 0)
            await Dispatcher.UIThread.InvokeAsync(DiscardRedoBranch);

        // Reindex existing entries
        foreach (var e in _collection) e.Index++;

        // Insert a lightweight placeholder (spinner on)
        var entry = new HistoryEntry
        {
            Index = 0,
            Kind = kind,
            Description = description,
            IsLoading = true
        };

        // UI: add to collections and enforce capacity
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _collection.Insert(0, entry);
            Timeline.Insert(0, entry);
            _cursor = 0;
            EnforceCapacity();
        });

        // UI thread clone is safest/fastest for RenderTargetBitmap on all platforms
        var snapshot = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            return CloneBitmap(bitmap);
        });

        // Populate entry and flip loading off
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            entry.Snapshot = snapshot;
            entry.IsLoading = false;

            // Notify list about updated item (if your UI needs it)
            var i = Timeline.IndexOf(entry);
            if (i >= 0) Timeline[i] = entry;
        });
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

        // Reset cursor to 0 (we are on the newest entry)
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

    public async Task<Bitmap?> Undo()
    {
        if (_cursor >= _collection.Count - 1) return null;
        _cursor++;
        return await RestoreSnapshot(_cursor);
    }

    public async Task<Bitmap?> Redo()
    {
        if (_cursor <= 0) return null;
        _cursor--;
        return await RestoreSnapshot(_cursor);
    }

    public async Task<Bitmap?> JumpTo(int index)
    {
        if (index < 0 || index >= _collection.Count) return null;
        _cursor = index;
        return await RestoreSnapshot(_cursor);
    }

    private async Task<Bitmap?> RestoreSnapshot(int index)
    {
        if (index < 0 || index >= _collection.Count)
            return null;

        var entry = _collection[index];
        var source = entry.Snapshot;
        if (source is null) return null;

        var bmp = await Dispatcher.UIThread.InvokeAsync(() => CloneBitmap(source));

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.ApplyBitmapAndRefresh(bmp, _vm);
        });
        return bmp;
    }

    public void Clear()
    {
        void Core()
        {
            foreach (var e in _collection)
            {
                e.Snapshot?.Dispose();
                e.Snapshot = null;
            }
        
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
                Width = 300,
                Height = 340,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(12),
                DataContext = _vm
            };

            UIHelper.GetMainView.MainGrid.Children.Add(Window);
        });        
    }

    public void Dispose()
    {
        Clear();
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


    public static Bitmap CloneBitmap(Bitmap source)
    {
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
}
