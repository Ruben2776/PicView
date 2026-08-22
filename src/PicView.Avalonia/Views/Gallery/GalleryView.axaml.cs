using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
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
                        GalleryItemsControl.ScrollToCenterOfCurrentItem();
                    }, DispatcherPriority.Background, tab.GetTabCancellation().Token);
                }
                else
                {
                    var index = e.NewStartingIndex;
                    var newItems = e.NewItems.ToArray();
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        int insertedIndex;
                        if (index >= 0 && index <= _galleryItems.Count)
                        {
                            _galleryItems.InsertRange(index, newItems);
                            insertedIndex = index;
                        }
                        else
                        {
                            insertedIndex = _galleryItems.Count;
                            _galleryItems.AddRange(newItems);
                        }

                        var needsScroll = false;
                        if (tab.NavigationIndex.Value >= insertedIndex && 
                            tab.NavigationIndex.Value < insertedIndex + newItems.Length)
                        {
                            GalleryItemsControl.CurrentItemIndex = tab.NavigationIndex.Value;
                            GalleryItemsControl.SelectedItemIndex = tab.NavigationIndex.Value;
                            needsScroll = true;
                        }

                        if (needsScroll)
                        {
                            Dispatcher.InvokeAsync(() =>
                            {
                                if (GalleryItemsControl.ItemsPanelRoot is null)
                                {
                                    return;
                                }
                                var targetIndex = tab.NavigationIndex.Value;
                                if (GalleryItemsControl.ItemsPanelRoot.Children.Count <= targetIndex ||
                                    GalleryItemsControl.ItemsPanelRoot.Children[targetIndex] is not ContentPresenter presenter)
                                {
                                    return;
                                }
                                var child = presenter.Child as GalleryItem; 
                                child?.SetCurrent(true);
                                child?.SetSelected(true);
                                GalleryItemsControl.ScrollToCenterOfCurrentItem();
                            }, DispatcherPriority.Loaded, tab.GetTabCancellation().Token);
                        }
                    }, DispatcherPriority.Background, tab.GetTabCancellation().Token);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                if (e.NewItems.IsEmpty)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _galleryItems.Clear();
                    }, DispatcherPriority.Background, tab.GetTabCancellation().Token);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.IsSingleItem)
                {
                    var oldItem = e.OldItem;
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _galleryItems.Remove(oldItem);
                        GalleryItemsControl.ScrollToCenterOfCurrentItem();  
                    }, DispatcherPriority.Render, tab.GetTabCancellation().Token);
                }
                else
                {
                    var oldItems = e.OldItems.ToArray();
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _galleryItems.RemoveAll(oldItems);
                        if (tab != null && oldItems.Any(item => tab.Model.FileInfo.FullName == item.FileInfo.FullName))
                        {
                            GalleryItemsControl.ScrollToCenterOfCurrentItem();
                        }
                    }, DispatcherPriority.Background, tab.GetTabCancellation().Token);
                }
                break;
            // Replace, Move
            default:
                break;
        }
    }
}