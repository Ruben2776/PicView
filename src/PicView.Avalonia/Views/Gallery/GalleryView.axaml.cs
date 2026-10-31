using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ObservableCollections;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Gallery;

public partial class GalleryView : GalleryAnimationControl
{
    private readonly AvaloniaList<GalleryItemViewModel> _galleryItems = [];
    private DisposableBag _disposables;

    /// <summary>
    /// 1 while a re-layout of the panel waits to run on the UI thread, so a burst of size batches costs one layout.
    /// </summary>
    private int _isRelayoutPosted;

    public GalleryView()
    {
        if (Application.Current.DataContext is not CoreViewModel)
        {
            return;
        }
        InitializeComponent();

        GalleryItemsControl.ItemsSource = _galleryItems;
    }

    /// <summary>
    /// Shows the gallery of <paramref name="tab"/> and follows it from now on. The image viewer that holds this view
    /// calls it once, as it is created, with the tab the viewer shows. The view is given the tab instead of looking up
    /// the active one, since a viewer is also created for a tab that is not active, and it doesn't wait for a
    /// DataContext, which is set once and never handled when it changes.
    /// </summary>
    public void Initialize(TabViewModel tab)
    {
        // The designer loads no XAML
        if (GalleryItemsControl is null)
        {
            return;
        }

        var gallery = tab.Gallery;
        gallery.GalleryItems.CollectionChanged += (in e) => OnGalleryItemsChanged(tab, in e);
        gallery.NavigateGalleryCommand.Subscribe(x =>
        {
            var direction = x switch
            {
                NavigateTo.Next => NavigationDirection.Right,
                NavigateTo.Previous => NavigationDirection.Left,
                NavigateTo.First => NavigationDirection.First,
                NavigateTo.Last => NavigationDirection.Last,
                NavigateTo.Up => NavigationDirection.Up,
                NavigateTo.Down => NavigationDirection.Down,
                _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
            };
            GalleryItemsControl.Navigate(direction);
        }, DebugHelper.LogError(nameof(GalleryView), nameof(gallery.NavigateGalleryCommand)))
            .AddTo(ref _disposables);

        gallery.ItemSpacing.Subscribe(_ =>
        {
            RecalculateGalleryIfNeeded(gallery);
        }, DebugHelper.LogError(nameof(GalleryView), nameof(gallery.ItemSpacing)))
        .AddTo(ref _disposables);
        gallery.LineSpacing.Subscribe(_ =>
        {
            RecalculateGalleryIfNeeded(gallery);
        }, DebugHelper.LogError(nameof(GalleryView), nameof(gallery.LineSpacing)))
        .AddTo(ref _disposables);

        gallery.SizesChanged.Subscribe(_ =>
        {
            PostRelayout();
        }, DebugHelper.LogError(nameof(GalleryView), nameof(gallery.SizesChanged)))
        .AddTo(ref _disposables);
    }

    private void RecalculateGalleryIfNeeded(GalleryViewModel gallery)
    {
        if (!gallery.IsGalleryExpanded.CurrentValue || IsInAnimation)
        {
            return;
        }

        if (GalleryItemsControl.ItemsPanelRoot is VirtualizingGallery galleryItemsPanel)
        {
            RunOnUI(() =>
            {
                galleryItemsPanel.InvalidateMeasure();
            }, DispatcherPriority.Render);

        }
    }

    private static void RunOnUI(Action action, DispatcherPriority priority, CancellationToken ct = default)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Invoke(action, priority, ct);
        }
    }

    private void OnGalleryItemsChanged(TabViewModel tab, in NotifyCollectionChangedEventArgs<GalleryItemViewModel> e)
    {
        var cancellationToken = tab.GetTabCancellation().Token;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.IsSingleItem)
                {
                    var newItem = e.NewItem;
                    var index = e.NewStartingIndex;
                    RunOnUI(() =>
                    {
                        if (index >= 0 && index <= _galleryItems.Count)
                        {
                            _galleryItems.Insert(index, newItem);
                        }
                        else
                        {
                            _galleryItems.Add(newItem);
                        }

                        SyncCurrentItem();
                    }, DispatcherPriority.Background, cancellationToken);
                }
                else
                {
                    var index = e.NewStartingIndex;
                    var newItems = e.NewItems.ToArray();
                    RunOnUI(() =>
                    {
                        if (index >= 0 && index <= _galleryItems.Count)
                        {
                            _galleryItems.InsertRange(index, newItems);
                        }
                        else
                        {
                            _galleryItems.AddRange(newItems);
                        }
                    }, DispatcherPriority.Background, cancellationToken);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                // Sorting resets the list as well as clearing it, so show what it holds now
                var resetItems = tab.Gallery.GalleryItems.ToArray();
                RunOnUI(() =>
                {
                    _galleryItems.Clear();
                    _galleryItems.AddRange(resetItems);
                }, DispatcherPriority.Render, CancellationToken.None);
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.IsSingleItem)
                {
                    var oldItem = e.OldItem;
                    RunOnUI(() =>
                    {
                        _galleryItems.Remove(oldItem);
                        SyncCurrentItem();
                    }, DispatcherPriority.Background, cancellationToken);
                }
                else
                {
                    var oldItems = e.OldItems.ToArray();
                    RunOnUI(() =>
                    {
                        _galleryItems.RemoveAll(oldItems);
                        SyncCurrentItem();
                    }, DispatcherPriority.Background, cancellationToken);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                var oldMoveIndex = e.OldStartingIndex;
                var newMoveIndex = e.NewStartingIndex;
                RunOnUI(() =>
                {
                    if (oldMoveIndex >= 0 && oldMoveIndex < _galleryItems.Count && 
                        newMoveIndex >= 0 && newMoveIndex < _galleryItems.Count)
                    {
                        _galleryItems.Move(oldMoveIndex, newMoveIndex);
                        SyncCurrentItem();
                    }
                }, DispatcherPriority.Background, cancellationToken);
                break;
            case NotifyCollectionChangedAction.Replace:
                var replaceIndex = e.NewStartingIndex;
                var replaceItem = e.NewItem;
                RunOnUI(() =>
                {
                    if (replaceIndex >= 0 && replaceIndex < _galleryItems.Count)
                    {
                        _galleryItems[replaceIndex] = replaceItem;
                    }
                }, DispatcherPriority.Loaded, cancellationToken);
                break;
        }
    }
    
    /// <summary>
    /// Marks the current item again. The tab's index can change before the items it points into do, as when a
    /// renamed file moves to its sorted place, which leaves the mark on the wrong item.
    /// </summary>
    private void SyncCurrentItem()
    {
        GalleryItemsControl.UpdateCurrentVisualState();
    }

    /// <summary>
    /// Lays out the panel again, since the sizes of its items changed. The sizes arrive in batches on another thread,
    /// and only one layout waits at a time, so a burst of batches costs one layout.
    /// </summary>
    private void PostRelayout()
    {
        if (Interlocked.Exchange(ref _isRelayoutPosted, 1) is 1)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            // Clear it first, so a batch that arrives from here on posts another layout
            Interlocked.Exchange(ref _isRelayoutPosted, 0);
            GalleryItemsControl.ItemsPanelRoot?.InvalidateMeasure();
        }, DispatcherPriority.Background);
    }

    private void ShowGalleryItemSizeSlider(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        mainWindow.AddGalleryItemSizeSlider();
    }
}