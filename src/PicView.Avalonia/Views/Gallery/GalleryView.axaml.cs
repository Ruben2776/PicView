using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Threading;
using ObservableCollections;
using PicView.Avalonia.CustomControls;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Gallery;

public partial class GalleryView : GalleryAnimationControl
{
    public GalleryView()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        InitializeComponent();

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
        });

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
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (index >= 0 && index <= GalleryItemsControl.Items.Count)
                        {
                            GalleryItemsControl.Items.Insert(index, newItem);
                        }
                        else
                        {
                            GalleryItemsControl.Items.Add(newItem);
                        }
                        GalleryItemsControl.ScrollToCenterOfCurrentItem();
                    },DispatcherPriority.Background);
                }
                else
                {
                    var index = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        var localIndex = index;
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (localIndex >= 0 && localIndex <= GalleryItemsControl.Items.Count)
                            {
                                GalleryItemsControl.Items.Insert(localIndex, item);
                                if (tab.NavigationIndex.Value != localIndex)
                                {
                                    return;
                                }

                                GalleryItemsControl.CurrentItemIndex = localIndex;
                                GalleryItemsControl.SelectedItemIndex = localIndex;
                                Dispatcher.Post(() =>
                                {
                                    if (GalleryItemsControl.ItemsPanelRoot.Children[localIndex] is not ContentPresenter
                                        presenter)
                                    {
                                        return;
                                    }

                                    var child = presenter.Child as GalleryItem; 
                                    child?.SetCurrent(true);
                                    child?.SetSelected(true);
                                    GalleryItemsControl.ScrollToCenterOfCurrentItem();
                                }, DispatcherPriority.Render);
                            }
                            else
                            {
                                GalleryItemsControl.Items.Add(item);
                            }
                        },DispatcherPriority.Background);
                        
                        if (index >= 0)
                        {
                            index++;
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                if (e.NewItems.IsEmpty)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        GalleryItemsControl.Items.Clear();
                    },DispatcherPriority.Background);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.IsSingleItem)
                {
                    var newItem = e.NewItem;
                    Dispatcher.UIThread.Post(() =>
                    {
                        GalleryItemsControl.Items.Remove(newItem);
                        GalleryItemsControl.ScrollToCenterOfCurrentItem();  
                    });
                }
                else
                {
                    foreach (var item in e.NewItems)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            GalleryItemsControl.Items.Remove(item);
                        },DispatcherPriority.Background);
                        if (tab.Model.FileInfo.FullName != item.FileInfo.FullName)
                        {
                            continue;
                        }

                        Dispatcher.UIThread.Post(() =>
                        {
                            GalleryItemsControl.ScrollToCenterOfCurrentItem();
                        });
                    }
                }
                break;
            // Replace, Move
            default:
                break;
        }
    }
}