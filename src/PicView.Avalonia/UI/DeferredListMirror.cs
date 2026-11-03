using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ObservableCollections;

namespace PicView.Avalonia.UI;

/// <summary>
///     A copy of an <see cref="ObservableList{T}" /> for a view to show, which takes each change through its own post.
///     A view bound to the list itself builds every row of a fill in one frame, so a menu opening with fifty new rows
///     stalls; posted at a priority below rendering, the rows are built between frames instead. Moves and inserts keep
///     their places, so a list patched in place stays patched in place in the view.
/// </summary>
public sealed class DeferredListMirror<T> : IDisposable
{
    private readonly ObservableList<T> _source;
    private readonly Action<Action> _post;

    /// <summary>
    ///     The items as the posts that have run so far leave them. Bind a view to this.
    /// </summary>
    public ObservableCollection<T> Items { get; }

    /// <param name="source">The list to follow. It may change on any thread.</param>
    /// <param name="post">Runs an action later, in order, on the thread that owns <see cref="Items" />.</param>
    public DeferredListMirror(ObservableList<T> source, Action<Action> post)
    {
        _source = source;
        _post = post;
        Items = new ObservableCollection<T>(source);
        _source.CollectionChanged += OnSourceChanged;
    }

    // Each post replays one change on Items. They run in the order the source changed, so the indices of each change
    // match Items as the earlier posts left it
    private void OnSourceChanged(in NotifyCollectionChangedEventArgs<T> e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.IsSingleItem)
                {
                    var item = e.NewItem;
                    var index = e.NewStartingIndex;
                    _post(() => Items.Insert(index, item));
                }
                else
                {
                    var start = e.NewStartingIndex;
                    var items = e.NewItems.ToArray();
                    for (var i = 0; i < items.Length; i++)
                    {
                        var item = items[i];
                        var index = start + i;
                        _post(() => Items.Insert(index, item));
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
            {
                var index = e.OldStartingIndex;
                var count = e.IsSingleItem ? 1 : e.OldItems.Length;
                for (var i = 0; i < count; i++)
                {
                    _post(() => Items.RemoveAt(index));
                }
                break;
            }

            case NotifyCollectionChangedAction.Replace:
            {
                var item = e.NewItem;
                var index = e.NewStartingIndex;
                _post(() => Items[index] = item);
                break;
            }

            case NotifyCollectionChangedAction.Move:
            {
                var from = e.OldStartingIndex;
                var to = e.NewStartingIndex;
                _post(() => Items.Move(from, to));
                break;
            }

            case NotifyCollectionChangedAction.Reset:
                _post(() => Items.Clear());
                break;
        }
    }

    public void Dispose() => _source.CollectionChanged -= OnSourceChanged;
}
