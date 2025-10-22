using System;
using System.Collections.Generic;
using System.Linq;
using ImageMagick;
using R3;
using Avalonia.Media.Imaging;

namespace PicView.Avalonia.History;

public sealed class HistoryManager : IDisposable
{
    private readonly int _capacity;
    private readonly MagickImageCollection _collection = new();
    private int _cursor = -1;
    private readonly BindableReactiveProperty<IReadOnlyList<HistoryEntry>> _timelineObservable = new(new List<HistoryEntry>());

    public HistoryManager(int capacity = 50)
    {
        _capacity = Math.Max(2, capacity);
    }

    public BindableReactiveProperty<IReadOnlyList<HistoryEntry>> TimelineObservable => _timelineObservable;
    public int Cursor => _cursor;

    public MagickImage? Current => _cursor >= 0 && _cursor < _collection.Count
        ? (MagickImage)_collection[_cursor]
        : null;

    // Adds a new edit snapshot as a new layer in the collection
    public void AddStep(EditKind kind, string description, MagickImage snapshot)
    {
        if (kind == EditKind.Open)
            Clear();

        // Trim any “future” frames (if we undid and then made a new edit)
        for (int i = _collection.Count - 1; i > _cursor; i--)
            _collection.RemoveAt(i);

        // Trim to capacity
        while (_collection.Count >= _capacity)
            _collection.RemoveAt(0);

        // Clone snapshot for storage
        var clone = snapshot.Clone();
        _collection.Add(clone);
        _cursor = _collection.Count - 1;

        // Build new timeline entries
        var newList = new List<HistoryEntry>(_collection.Count);
        for (int i = 0; i < _collection.Count; i++)
        {
            var entry = new HistoryEntry
            {
                Index = i,
                Kind = i == _cursor ? kind : _timelineObservable.Value.ElementAtOrDefault(i)?.Kind ?? EditKind.Other,
                Description = i == _cursor ? description : _timelineObservable.Value.ElementAtOrDefault(i)?.Description ?? "",
                Snapshot = (MagickImage)_collection[i],
            };

            // Build cached thumbnail for the latest entry only (others keep existing)
            if (i == _cursor)
            {
                try
                {
                    // Create small 100px thumbnail off-thread
                    using var thumbClone = clone.Clone();
                    var max = 100;
                    if (thumbClone.Width > thumbClone.Height)
                        thumbClone.Resize((uint)max, 0);
                    else
                        thumbClone.Resize(0, (uint)max);

                    using var ms = new MemoryStream();
                    thumbClone.Format = MagickFormat.Png;
                    thumbClone.Write(ms);
                    ms.Position = 0;

                    entry.CachedThumbnail = new Bitmap(ms);
                }
                catch
                {
                    entry.CachedThumbnail = null;
                }
            }
            else
            {
                entry.CachedThumbnail = _timelineObservable.Value.ElementAtOrDefault(i)?.CachedThumbnail;
            }

            newList.Add(entry);
        }

        _timelineObservable.Value = newList;
    }

    public MagickImage? Undo()
    {
        if (_cursor <= 0)
            return null;

        _cursor--;
        _timelineObservable.Value = UpdateTimelineDescriptions();
        return (MagickImage)_collection[_cursor];
    }

    public MagickImage? Redo()
    {
        if (_cursor >= _collection.Count - 1)
            return null;

        _cursor++;
        _timelineObservable.Value = UpdateTimelineDescriptions();
        return (MagickImage)_collection[_cursor];
    }

    public MagickImage? JumpTo(int index)
    {
        if (index < 0 || index >= _collection.Count)
            return null;

        _cursor = index;
        _timelineObservable.Value = UpdateTimelineDescriptions();
        return (MagickImage)_collection[_cursor];
    }

    private List<HistoryEntry> UpdateTimelineDescriptions()
    {
        return _collection
            .Select((img, idx) => new HistoryEntry
            {
                Index = idx,
                Kind = EditKind.Other,
                Description = idx == _cursor ? "Active" : "",
                Snapshot = (MagickImage)img,
            })
            .ToList();
    }

    public void Clear()
    {
        _collection.Clear();
        _cursor = -1;
        _timelineObservable.Value = new List<HistoryEntry>();
    }

    public int Count() => _collection.Count();
    
    public void Dispose()
    {
        _collection.Dispose();
    }
}
