namespace PicView.Core.Navigation;

/// <summary>
/// A step Back or Forward has marked.
/// </summary>
/// <param name="Target">The entry to show.</param>
/// <param name="Id">
/// Names the pending step for <see cref="ViewHistory.EndStep"/>. 0 when the target is what the tab already shows,
/// since no step is left to end.
/// </param>
public readonly record struct ViewHistoryStep(ViewHistoryEntry Target, int Id);

/// <summary>
/// What one tab has shown, oldest to newest, with a cursor on what it shows now. Back and Forward walk it as a browser does.
/// </summary>
/// <remarks>
/// A step only marks a target. The cursor moves when the tab shows that entry and records it, so the cursor follows what
/// the tab shows, never what was asked for. Every member takes the lock, since a load records from whichever thread it
/// finishes on. Neighbors are never the same entry: a repeat is not recorded, and a removal that would leave two merges
/// them.
/// </remarks>
/// <param name="fileExists">
/// Whether a file is still on disk, which a step asks to skip the entries whose file is gone. Defaults to
/// <see cref="File.Exists"/>.
/// </param>
public sealed class ViewHistory(Func<string, bool>? fileExists = null)
{
    /// <summary>
    /// The most entries kept. Recording one more drops the oldest.
    /// </summary>
    public const int MaxEntries = 100;

    /// <summary>
    /// The most Single Images among them, since each keeps its image in memory. Recording one more drops the oldest
    /// Single Image, wherever it sits.
    /// </summary>
    public const int MaxSingleImages = 10;

    // Held for a few in-memory list operations only, never across disk access or an await
    private readonly Lock _lock = new();

    // Sized for the entry that pushes the oldest one out, so recording never grows the list
    private readonly List<ViewHistoryEntry> _entries = new(MaxEntries + 1);
    private readonly Func<string, bool> _fileExists = fileExists ?? File.Exists;
    private int _cursor = -1;

    // The entry the pending step is going to, -1 when no step is pending
    private int _target = -1;

    // Names the pending step, 0 when no step is pending
    private int _targetId;
    private int _lastStepId;

    /// <summary>
    /// The number of entries.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>
    /// The index of the entry the tab shows, -1 when there are no entries.
    /// </summary>
    public int Cursor
    {
        get
        {
            lock (_lock)
            {
                return _cursor;
            }
        }
    }

    /// <summary>
    /// The entry the tab shows, null when there are no entries.
    /// </summary>
    public ViewHistoryEntry? Current
    {
        get
        {
            lock (_lock)
            {
                return _cursor >= 0 ? _entries[_cursor] : null;
            }
        }
    }

    /// <summary>
    /// A copy of the entries, the oldest first.
    /// </summary>
    public ViewHistoryEntry[] Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToArray();
            }
        }
    }

    /// <summary>
    /// Records that the tab shows <paramref name="entry"/>. Call it after the image is shown. It reads no disk and
    /// allocates nothing.
    /// </summary>
    /// <returns>
    /// False for a repeat: the cursor's entry shown again. True when the entry was added, or the cursor moved to it.
    /// </returns>
    public bool Record(in ViewHistoryEntry entry)
    {
        lock (_lock)
        {
            if (_target >= 0)
            {
                // A step is under way, so the entry may be one on the way to its target. This comes before the repeat
                // check, since the target can be another view of the entry the cursor is on, as in A, B, A
                var onTheWay = IndexTowardsTarget(entry);
                if (onTheWay >= 0)
                {
                    _cursor = onTheWay;
                    if (onTheWay == _target)
                    {
                        EndPendingStep();
                    }
                    return true;
                }
            }

            // A reload, a crop or a file changed on disk shows the same entry again. A pending step stays pending
            if (_cursor >= 0 && _entries[_cursor].Matches(entry))
            {
                return false;
            }

            // Something else shows, so the step ends. As in a browser, what is new drops the entries after the cursor
            EndPendingStep();
            _entries.RemoveRange(_cursor + 1, _entries.Count - _cursor - 1);
            _entries.Add(entry);
            _cursor = _entries.Count - 1;

            if (_entries.Count > MaxEntries)
            {
                RemoveAt(0);
            }

            if (entry.Kind is ViewHistoryKind.SingleImage)
            {
                DropOldestSingleImageOverTheCap();
            }
            return true;
        }
    }

    /// <summary>
    /// Marks the entry a step back or forward away as the target, and does not move the cursor. A step taken while another
    /// is under way counts from the earlier step's target, not from the cursor. Entries whose file is gone are removed
    /// and skipped. It checks files on disk, so call it off the UI thread.
    /// </summary>
    /// <returns>The step, or null when there is no entry that way.</returns>
    public ViewHistoryStep? BeginStep(bool forwards)
    {
        while (true)
        {
            int index;
            ViewHistoryEntry candidate;
            lock (_lock)
            {
                index = StepIndex(forwards);
                // The cursor is -1 with entries left when the first was removed, so the first is still ahead of it
                if (index < 0 || index >= _entries.Count)
                {
                    return null;
                }
                candidate = _entries[index];
            }

            // Outside the lock, so a load that records meanwhile never waits for the disk
            var exists = candidate.FilePath is not { } path || _fileExists(path);

            lock (_lock)
            {
                // An overlapping BeginStep may have marked a target meanwhile, or a record may have moved the cursor.
                // The entry must still be a step from where a step counts now, or both steps would mark it
                if (index >= _entries.Count || index != StepIndex(forwards) || !_entries[index].Matches(candidate))
                {
                    // The list or the step under way changed meanwhile; look again
                    continue;
                }
                if (!exists)
                {
                    RemoveAt(index);
                    continue;
                }
                if (index == _cursor)
                {
                    // The tab already shows it, so no target is left to reach
                    EndPendingStep();
                    return new ViewHistoryStep(candidate, 0);
                }
                _target = index;
                _targetId = ++_lastStepId;
                return new ViewHistoryStep(candidate, _targetId);
            }
        }
    }

    /// <summary>
    /// Ends <paramref name="step"/> once its load is over, shown or not, so the next step counts from the cursor again.
    /// Does nothing when the step has ended already.
    /// </summary>
    public void EndStep(ViewHistoryStep step)
    {
        lock (_lock)
        {
            if (step.Id != 0 && step.Id == _targetId)
            {
                EndPendingStep();
            }
        }
    }

    // The members below run with the lock held

    // Where a step this way goes: one entry from the target of the step under way, else from the cursor
    private int StepIndex(bool forwards)
    {
        var origin = _target >= 0 ? _target : _cursor;
        return forwards ? origin + 1 : origin - 1;
    }

    private void EndPendingStep()
    {
        _target = -1;
        _targetId = 0;
    }

    /// <summary>
    /// Removes an entry. The cursor and the target move down when they were at it or after it, so a cursor on the removed
    /// entry lands on the one before it. If that leaves two neighbors for the same thing, they merge.
    /// </summary>
    private void RemoveAt(int index)
    {
        _entries.RemoveAt(index);

        if (index <= _cursor)
        {
            _cursor--;
        }

        if (index <= _target)
        {
            _target--;
        }

        // A step with no target, or with its target under the cursor, has nothing left to reach
        if (_target < 0 || _target == _cursor)
        {
            EndPendingStep();
        }

        // Two neighbors for the same thing would leave Back stuck on the one the tab shows. The later one goes, so a
        // cursor or a target on either lands on the merged entry
        if (index > 0 && index < _entries.Count && _entries[index - 1].Matches(_entries[index]))
        {
            RemoveAt(index);
        }
    }

    /// <summary>
    /// Looks from the entry next to the cursor towards the target, the target included, for an entry that matches.
    /// </summary>
    /// <returns>Its index, or -1 when none matches.</returns>
    private int IndexTowardsTarget(in ViewHistoryEntry entry)
    {
        if (_target > _cursor)
        {
            for (var i = _cursor + 1; i <= _target; i++)
            {
                if (_entries[i].Matches(entry))
                {
                    return i;
                }
            }
        }
        else
        {
            for (var i = _cursor - 1; i >= _target; i--)
            {
                if (_entries[i].Matches(entry))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    // Single Images keep their images in memory, so only a few of them are kept, wherever they sit
    private void DropOldestSingleImageOverTheCap()
    {
        var count = 0;
        var oldest = -1;
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Kind is not ViewHistoryKind.SingleImage)
            {
                continue;
            }

            count++;
            if (oldest < 0)
            {
                oldest = i;
            }
        }

        if (count > MaxSingleImages)
        {
            RemoveAt(oldest);
        }
    }
}
