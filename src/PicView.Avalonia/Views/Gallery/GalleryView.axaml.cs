using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Threading;
using ObservableCollections;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Gallery;

public partial class GalleryView : GalleryAnimationControl
{
    private readonly AvaloniaList<GalleryItemViewModel> _galleryItems = [];

    public GalleryView()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        InitializeComponent();

        GalleryItemsControl.ItemsSource = _galleryItems;

        var gallery = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Gallery;
        gallery.GalleryItems.CollectionChanged += CurrentValueOnCollectionChanged;
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
        }, DebugHelper.LogError(nameof(GalleryView), nameof(gallery.NavigateGalleryCommand)));

         if (Settings.Gallery.IsGalleryDocked)
         {
             Height = Settings.Gallery.DockedGalleryItemSize + 2 + SizeDefaults.ScrollbarSize;
         }
         else
         {
             Height = 0;
         }
    }

    private void CurrentValueOnCollectionChanged(in NotifyCollectionChangedEventArgs<GalleryItemViewModel> e)
    {
        var tab = Dispatcher.UIThread.Invoke(() =>
            Application.Current.DataContext is not CoreViewModel core ? null : core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue);
        var cancellationToken = tab.GetTabCancellation().Token;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.IsSingleItem)
                {
                    var newItem = e.NewItem;
                    var index = e.NewStartingIndex;
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (index >= 0 && index <= _galleryItems.Count)
                        {
                            _galleryItems.Insert(index, newItem);
                        }
                        else
                        {
                            _galleryItems.Add(newItem);
                        }
                        SyncGalleryViewerScroll(tab);
                    }, DispatcherPriority.Loaded, cancellationToken);
                }
                else
                {
                    var index = e.NewStartingIndex;
                    var newItems = e.NewItems.ToArray();
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (index >= 0 && index <= _galleryItems.Count)
                        {
                            _galleryItems.InsertRange(index, newItems);
                        }
                        else
                        {
                            _galleryItems.AddRange(newItems);
                        }

                        SyncGalleryViewerScroll(tab);
                    }, DispatcherPriority.Loaded, cancellationToken);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                var gallery = tab.Gallery;
                var currentItems = gallery.GalleryItems.ToArray();
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _galleryItems.Clear();
                    if (currentItems.Length <= 0)
                    {
                        return;
                    }

                    SyncGalleryViewerScroll(tab);
                }, DispatcherPriority.Loaded, cancellationToken);
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.IsSingleItem)
                {
                    var oldItem = e.OldItem;
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _galleryItems.Remove(oldItem);
                        GalleryItemsControl.ScrollToCenterOfCurrentItem();  
                    }, DispatcherPriority.Loaded, cancellationToken);
                }
                else
                {
                    var oldItems = e.OldItems.ToArray();
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _galleryItems.RemoveAll(oldItems);
                        if (oldItems.Any(item => tab.Model.FileInfo?.FullName == item.FileInfo?.FullName))
                        {
                            GalleryItemsControl.ScrollToCenterOfCurrentItem();
                        }
                    }, DispatcherPriority.Loaded, cancellationToken);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                var oldMoveIndex = e.OldStartingIndex;
                var newMoveIndex = e.NewStartingIndex;
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (oldMoveIndex >= 0 && oldMoveIndex < _galleryItems.Count && 
                        newMoveIndex >= 0 && newMoveIndex < _galleryItems.Count)
                    {
                        _galleryItems.Move(oldMoveIndex, newMoveIndex);
                        GalleryItemsControl.ScrollToCenterOfCurrentItem();
                    }
                }, DispatcherPriority.Loaded, cancellationToken);
                break;
            case NotifyCollectionChangedAction.Replace:
                var replaceIndex = e.NewStartingIndex;
                var replaceItem = e.NewItem;
                if (replaceIndex < 0 && replaceIndex > _galleryItems.Count)
                {
                    return;
                }
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _galleryItems[replaceIndex] = replaceItem;
                }, DispatcherPriority.Loaded, cancellationToken);
                Dispatcher.UIThread.Post(() =>
                {
                    GalleryItemsControl.ScrollToCenterOfCurrentItem();
                }, DispatcherPriority.Loaded);
                break;
        }
    }

    private void SyncGalleryViewerScroll(TabViewModel tab)
    {
        GalleryItemsControl.CurrentItemIndex = tab.NavigationIndex.Value;
        GalleryItemsControl.SelectedItemIndex = tab.NavigationIndex.Value;
        GalleryItemsControl.ScrollToCenterOfCurrentItem();
    }
}