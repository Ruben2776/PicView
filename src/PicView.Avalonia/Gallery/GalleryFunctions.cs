using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Gallery;
using PicView.Core.Localization;

namespace PicView.Avalonia.Gallery;
public static class GalleryFunctions
{
    #region Gallery toggle
    public static bool IsFullGalleryOpen { get; private set; }
    public static bool IsBottomGalleryOpen { get; private set; }

    public static async Task ToggleGallery(MainViewModel vm)
    {
        if (vm is null || !NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        
        UIHelper.CloseMenus(vm);
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            // Showing bottom gallery is enabled
            IsBottomGalleryOpen = true;
            if (IsFullGalleryOpen)
            {
                // Switch to bottom gallery
                IsFullGalleryOpen = false;
                vm.GalleryMode = GalleryMode.FullToBottom;
                vm.GetGalleryItemHeight = vm.GetBottomGalleryItemHeight;
            }
            else
            {
                // Switch to full gallery
                IsFullGalleryOpen = true;
                vm.GalleryMode = GalleryMode.BottomToFull;
                vm.GetGalleryItemHeight = vm.GetFullGalleryItemHeight;
            }
        }
        else
        {
            IsBottomGalleryOpen = false;
            if (IsFullGalleryOpen)
            {
                // close full gallery
                IsFullGalleryOpen = false;
                vm.GalleryMode = GalleryMode.FullToClosed;
            }
            else
            {
                // open full gallery
                IsFullGalleryOpen = true;
                vm.GalleryMode = GalleryMode.ClosedToFull;
                vm.GetGalleryItemHeight = vm.GetFullGalleryItemHeight;
            }
        }
        _ = Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.ImagePaths[0])));
        await SettingsHelper.SaveSettingsAsync();
    }

    public static async Task OpenCloseBottomGallery(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        UIHelper.CloseMenus(vm);
        
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            SettingsHelper.Settings.Gallery.IsBottomGalleryShown = false;
            IsFullGalleryOpen = false;
            IsBottomGalleryOpen = false;
            vm.GalleryMode = GalleryMode.BottomToClosed;
            vm.GetIsShowingBottomGalleryTranslation = TranslationHelper.Translation.ShowBottomGallery;
            await SettingsHelper.SaveSettingsAsync();
            return;
        }

        IsBottomGalleryOpen = true;
        IsFullGalleryOpen = false;
        SettingsHelper.Settings.Gallery.IsBottomGalleryShown = true;
        if (NavigationHelper.CanNavigate(vm))
        {
            vm.GalleryMode = GalleryMode.ClosedToBottom;
        }
        vm.GetIsShowingBottomGalleryTranslation = TranslationHelper.Translation.HideBottomGallery;
        await SettingsHelper.SaveSettingsAsync();
        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        await Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.ImagePaths[0])));
    }

    public static void OpenBottomGallery(MainViewModel vm)
    {
        IsBottomGalleryOpen = true;
        vm.GalleryMode = GalleryMode.ClosedToBottom;
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
    }
    
    public static async Task CloseGallery(MainViewModel vm)
    {
        if (IsFullGalleryOpen)
        {
            await ToggleGallery(vm);
        }
        else
        {
            await OpenCloseBottomGallery(vm);
        }
    }
    
    #endregion
    
    #region Sorting
    
    private readonly struct TempGalleryItem(
        string fileLocation,
        string fileName,
        string fileSize,
        string fileDate,
        IImage source)
    {
        internal readonly string FileLocation = fileLocation;
        internal readonly string FileName = fileName;
        internal readonly string FileSize = fileSize;
        internal readonly string FileDate = fileDate;
        internal IImage Source { get; } = source;
    }
    
     public static async Task SortGalleryItems(List<string> files, MainViewModel vm)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        var mainView = UIHelper.GetMainView;

        var galleryListBox = mainView.GalleryView.GalleryListBox;
        if (galleryListBox == null) return;
        if (galleryListBox.Items.Count < 0)
        {
            return;
        }
        var initialDirectory = Path.GetDirectoryName(vm.FileInfo.FullName);
        try
        {
            while (GalleryLoad.IsLoading)
            {
                await Task.Delay(200, cancellationTokenSource.Token);
                if (initialDirectory == Path.GetDirectoryName(files[0]))
                {
                    continue;
                }

                // Directory changed, cancel the operation
                await cancellationTokenSource.CancelAsync();
                return;
            }
            
            var thumbs = new List<TempGalleryItem>();
            for (var i = 0; i < files.Count; i++)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (galleryListBox.Items.Count <= i)
                        return;
                    if (galleryListBox.Items[i] is not GalleryItem galleryItem)
                        return;
                    
                    thumbs.Add(new TempGalleryItem(
                        galleryItem.FileLocation.Text,
                        galleryItem.FileName.Text,
                        galleryItem.FileSize.Text,
                        galleryItem.FileDate.Text,
                        galleryItem.GalleryImage.Source));
                }, DispatcherPriority.Render, cancellationTokenSource.Token);
                if (initialDirectory != Path.GetDirectoryName(files[0]))
                {
                    // Directory changed, cancel the operation
                    await cancellationTokenSource.CancelAsync();
                    return;
                }
            }
            await Dispatcher.UIThread.InvokeAsync(() =>
                thumbs = thumbs.OrderBySequence(files, x => x.FileLocation).ToList());
            vm.SelectedGalleryItemIndex = -1;

            for (var i = 0; i < files.Count; i++)
            {
                var index = i;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (galleryListBox.Items[index] is not GalleryItem galleryItem)
                        return;
                    
                    galleryItem.FileLocation.Text = thumbs[index].FileLocation;
                    galleryItem.FileName.Text = thumbs[index].FileName;
                    galleryItem.FileSize.Text = thumbs[index].FileSize;
                    galleryItem.FileDate.Text = thumbs[index].FileDate;
                    galleryItem.GalleryImage.Source = thumbs[index].Source;
                }, DispatcherPriority.Background, cancellationTokenSource.Token);

                if (initialDirectory != Path.GetDirectoryName(files[0]))
                {
                    // Directory changed, cancel the operation
                    await cancellationTokenSource.CancelAsync();
                    return;
                }
            }
            vm.SelectedGalleryItemIndex = files.IndexOf(files[vm.ImageIterator.CurrentIndex]);
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        }
        catch (TaskCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                galleryListBox.Items.Clear();
            });
        }
        catch (Exception e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                galleryListBox.Items.Clear();
            });
            await TooltipHelper.ShowTooltipMessageAsync(e.Message);
#if DEBUG
            Console.WriteLine(e);
#endif
        }

    }
     #endregion

     public static bool RemoveGalleryItem(int index, MainViewModel? vm)
     {
         var mainView = UIHelper.GetMainView;

         var galleryListBox = mainView.GalleryView.GalleryListBox;
         if (galleryListBox == null) 
             return false;

         if (galleryListBox.Items.Count <= index)
         {
             return false;
         }

         if (galleryListBox.Items.Count < 0 || index >= galleryListBox.ItemCount)
         {
             return false;
         }

         if (galleryListBox.Items.Count <= 0 || index >= galleryListBox.Items.Count)
         {
             return false;
         }

         if (Dispatcher.UIThread.CheckAccess())
         {
             galleryListBox.Items.RemoveAt(index);
         }
         else
         {
             Dispatcher.UIThread.InvokeAsync(() =>
             {
                 galleryListBox.Items.RemoveAt(index);
             });
         }
         if (vm != null)
         {
             vm.SelectedGalleryItemIndex = vm.ImageIterator.CurrentIndex;
         }

         return true;

     }

     public static async Task<bool> AddGalleryItem(int index, FileInfo fileInfo, MainViewModel? vm)
     {
         var mainView = UIHelper.GetMainView;

         var galleryListBox = mainView.GalleryView.GalleryListBox;
         if (galleryListBox == null) 
             return false;

         if (galleryListBox.Items.Count <= index)
         {
             return false;
         }

         if (galleryListBox.Items.Count < 0 || index >= galleryListBox.ItemCount)
         {
             return false;
         }

         if (galleryListBox.Items.Count <= 0 || index >= galleryListBox.Items.Count)
         {
             return false;
         }

         GalleryItem? galleryItem;
         var thumb = await GetThumbnails.GetThumbAsync(fileInfo.FullName, (uint)vm.GetGalleryItemHeight, fileInfo);
         var galleryThumbInfo = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(fileInfo);
         try
         {
             await Dispatcher.UIThread.InvokeAsync(() =>
             {
                 galleryItem = new GalleryItem
                 {
                     FileLocation =
                     {
                         Text = galleryThumbInfo.FileLocation
                     },
                     FileDate =
                     {
                         Text = galleryThumbInfo.FileDate
                     },
                     FileSize =
                     {
                         Text = galleryThumbInfo.FileSize
                     },
                     FileName =
                     {
                         Text = galleryThumbInfo.FileName
                     }
                 };
                 galleryItem.PointerPressed += async (_, _) =>
                 {
                     if (IsFullGalleryOpen)
                     {
                         await ToggleGallery(vm);
                     }
                     await vm.ImageIterator.IterateToIndex(vm.ImageIterator.ImagePaths.IndexOf(fileInfo.FullName)).ConfigureAwait(false);
                 };
                 galleryListBox.Items.Insert(index, galleryItem);
                 var isSvg = fileInfo.Extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ||
                             fileInfo.Extension.Equals(".svgz", StringComparison.OrdinalIgnoreCase);
                 if (isSvg)
                 {
                     galleryItem.GalleryImage.Source = new SvgImage
                         { Source = SvgSource.Load(fileInfo.FullName) };
                 }
                 else if (thumb is not null)
                 {
                     galleryItem.GalleryImage.Source = thumb;
                 }
             }, DispatcherPriority.Render);
             return true;
         }
         catch (Exception exception)
         {
#if DEBUG
             Console.WriteLine(exception);
#endif
         }
         return false;
     }

     public static void Clear()
     {
         if (Dispatcher.UIThread.CheckAccess())
         {
             ClearItems();
         }
         else
         {
             Dispatcher.UIThread.Post(ClearItems);
         }
#if DEBUG
         Console.WriteLine("Gallery items cleared");
#endif
         
         return;
         void ClearItems()
         {
             try
             {
                 var mainView = UIHelper.GetMainView;

                 var galleryListBox = mainView.GalleryView.GalleryListBox;
                 if (galleryListBox == null) 
                     return;
                 for (var i = 0; i < galleryListBox.ItemCount; i++)
                 {
                     if (galleryListBox.Items[i] is not GalleryItem galleryItem) 
                         continue;
                     if (galleryItem.GalleryImage.Source is IDisposable galleryImage)
                     {
                         galleryImage.Dispose();
                     }
                     galleryListBox.Items.Remove(galleryItem);
                 }
                 galleryListBox.Items.Clear();
             }
             catch (Exception e)
             {
#if DEBUG
                     Console.WriteLine(e);
#endif
             }
         }
     }
}
