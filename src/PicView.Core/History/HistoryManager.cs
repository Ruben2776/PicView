using System;
using System.Collections.Generic;
using ImageMagick;
using R3;

namespace PicView.Core.History;

public sealed class HistoryManager : IDisposable
{
    private readonly int _capacity;
    private readonly List<HistoryEntry> _timeline = new();   // linear timeline
     private readonly BindableReactiveProperty<IReadOnlyList<HistoryEntry>> _timelineObservable = new(new List<HistoryEntry>());
    private int _cursor = -1;                                 // index of current state

    public HistoryManager(int capacity = 20)
    {
        _capacity = Math.Max(2, capacity);
    }


    public int Count => _timeline.Count;
    public int Cursor => _cursor;
    public IReadOnlyList<HistoryEntry> Timeline => _timeline;
    public BindableReactiveProperty<IReadOnlyList<HistoryEntry>> TimelineObservable => _timelineObservable;


    public void Clear()
    {
        foreach (var e in _timeline) e.Snapshot.Dispose();
        _timeline.Clear();
        _cursor = -1;
    }

    /// <summary>Add a new step AFTER the current cursor, truncating any "redo" tail.</summary>
    public void AddStep(HistoryEntry entry)
    {
        // When new step occurs after undo, drop all future states
        for (int i = _timeline.Count - 1; i > _cursor; i--)
        {
            _timeline[i].Snapshot.Dispose();
            _timeline.RemoveAt(i);
        }

        _timeline.Add(entry);
        _cursor = _timeline.Count - 1;

        // Enforce capacity: remove oldest until size <= capacity
        while (_timeline.Count > _capacity)
        {
            _timeline[0].Snapshot.Dispose();
            _timeline.RemoveAt(0);
            _cursor--; // shift cursor left
        }
        if (_cursor < 0 && _timeline.Count > 0) _cursor = 0;

        _timelineObservable.Value = _timeline.ToList();
    }

    public bool CanUndo => _cursor > 0;
    public bool CanRedo => _cursor >= 0 && _cursor < _timeline.Count - 1;

    public HistoryEntry? Undo()
    {
        if (!CanUndo) return null;
        _cursor--;
        return _timeline[_cursor];
    }

    public HistoryEntry? Redo()
    {
        if (!CanRedo) return null;
        _cursor++;
        return _timeline[_cursor];
    }

    public HistoryEntry? JumpTo(int index)
    {
        if (index < 0 || index >= _timeline.Count) return null;
        _cursor = index;
        return _timeline[_cursor];
    }

    public HistoryEntry? Current => (_cursor >= 0 && _cursor < _timeline.Count) ? _timeline[_cursor] : null;

    public void Dispose() => Clear();
}

