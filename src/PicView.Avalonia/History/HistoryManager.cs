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
    private readonly SemaphoreSlim _snapshotLock = new(1, 1);

    public ObservableCollection<HistoryEntry> Timeline { get; } = new();

    private readonly MainViewModel _vm;

    public HistoryWindowViewModel? WindowVm { get; private set; }
    public HistoryWindow? Window { get; private set; }

    public ReactiveCommand CloseCommand { get; } 

    public HistoryManager(MainViewModel vm, int capacity = 50)
    {
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        _capacity = Math.Max(2, capacity);

        CloseCommand = new ReactiveCommand(_ => Hide());
    }

    public async Task AddSnapshot(EditKind kind, string description, Bitmap? bitmap = null)
    {
        if (kind == EditKind.Open)
        {
            if (Dispatcher.UIThread.CheckAccess())
                Clear();
            else
                await Dispatcher.UIThread.InvokeAsync(Clear);
        }

        bitmap ??= (Bitmap?)_vm.PicViewer.ImageSource.Value;
        if (bitmap is null)
            return;

        foreach (var e in _collection)
            e.Index++;

        var placeholder = new HistoryEntry
        {
            Index = 0,
            Kind = kind,
            Description = description
        };

        _collection.Insert(0, placeholder);
        _cursor = 0;

        Dispatcher.UIThread.Post(() => Timeline.Insert(0, placeholder));

        _ = Task.Run(async () =>
        {
            await _snapshotLock.WaitAsync();
            try
            {
                var encoded = await EncodeBitmapAsync(bitmap);
                var thumb = BuildThumbnail(bitmap);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    placeholder.EncodedPng = encoded;
                    placeholder.CachedThumbnail = thumb;
                    placeholder.IsLoading = false;

                    var i = Timeline.IndexOf(placeholder);
                    if (i >= 0)
                        Timeline[i] = placeholder;
                });
            }
            finally
            {
                _snapshotLock.Release();
            }
        });
    }


    private static async Task<byte[]> EncodeBitmapAsync(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        await Task.Run(() => bmp.Save(ms));
        return ms.ToArray();
    }

    private static Bitmap? BuildThumbnail(Bitmap bmp)
    {
        try
        {
            var scale = 100.0 / Math.Max(bmp.PixelSize.Width, bmp.PixelSize.Height);
            var width = Math.Max(1, (int)(bmp.PixelSize.Width * scale));
            var height = Math.Max(1, (int)(bmp.PixelSize.Height * scale));
            return bmp.CreateScaledBitmap(new PixelSize(width, height));
        }
        catch
        {
            return null;
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
        if (entry.EncodedPng is null)
            return null;

        var bmp = await Task.Run(() =>
        {
            try
            {
                using var ms = new MemoryStream(entry.EncodedPng);
                return new Bitmap(ms);
            }
            catch
            {
                return null;
            }
        });

        if (bmp is null)
            return null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ((Bitmap?)_vm.PicViewer.ImageSource.Value)?.Dispose();
            _vm.PicViewer.ImageSource.Value = bmp;
            _vm.PicViewer.HasChanges.Value = true;
        });

        return bmp;
    }

    public void Clear()
    {
        foreach (var e in _collection)
            e.CachedThumbnail?.Dispose();

        _collection.Clear();
        _cursor = -1;
        Timeline.Clear();
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
        _snapshotLock.Dispose();
    }

    public void Hide()
    {
        UIHelper.GetMainView.MainGrid.Children.Remove(Window);
    }

}
