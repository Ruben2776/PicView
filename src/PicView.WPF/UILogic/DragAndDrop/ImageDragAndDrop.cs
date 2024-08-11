using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.FileHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.Views.UserControls.Misc;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using PicView.Core.Localization;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.ImageHandling.Thumbnails;
using static PicView.WPF.UILogic.Tooltip;

namespace PicView.WPF.UILogic.DragAndDrop;

internal static class ImageDragAndDrop
{
    /// <summary>
    /// Backup of image
    /// </summary>
    private static DragDropOverlay? _dropOverlay;
    private static bool _dragging;

    /// <summary>
    /// Show image or thumbnail preview on drag enter
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static async Task Image_DragEnter(object sender, DragEventArgs e)
    {
        if (GalleryFunctions.IsGalleryOpen)
            return;

        UIElement? element = null;
        _dragging = true;
        string? getImage = null;

        if (e.Data.GetData(DataFormats.FileDrop, true) is not string[] files)
        {
            var data = e.Data.GetData(DataFormats.Text);

            if (data != null) // Check if from web)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    element = new LinkChain();
                });
            }
        }
        else if (Directory.Exists(files[0]))
        {
            if (SettingsHelper.Settings.Sorting.IncludeSubDirectories || Directory.GetFiles(files[0]).Length > 0)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    element = new FolderIcon();
                });
            }
        }
        else if (files[0].IsArchive())
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => { element = new ZipIcon(); });
        }
        else if (files[0].IsSupported())
        {
            // Check if same file
            if (files.Length == 1 && Pics.Count > 0 && FolderIndex < Pics.Count)
            {
                if (files[0] == Pics[FolderIndex])
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(RemoveDragOverlay);
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }
            }

            getImage = files[0];
        }
        else return;

        if (_dragging)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(AddDragOverlay);
        }

        // Tell that it's succeeded
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;

        if (getImage is not null && _dragging)
        {
            var actualWidth = 0;
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                actualWidth = (int)ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
            });
            var thumb = await GetBitmapSourceThumbAsync(getImage, actualWidth).ConfigureAwait(false);
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                element = new DragDropOverlayPic(thumb);
            });
        }

        if (element == null) return;
        if (_dropOverlay != null && _dragging)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => { UpdateDragOverlay(element); });
        }
        _dragging = false;
    }

    /// <summary>
    /// Logic for handling when the cursor leaves drag area
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static void Image_DragLeave(object sender, DragEventArgs e)
    {
        _dragging = false;
        
        // Switch to previous image if available
        RemoveDragOverlay();
        if (ConfigureWindows.GetMainWindow.TitleText.Text ==
                 TranslationHelper.Translation.NoImage)
        {
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
        }

        CloseToolTipMessage();
    }

    /// <summary>
    /// Logic for handling the drop event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static async Task Image_Drop(object sender, DragEventArgs e)
    {
        if (GalleryFunctions.IsGalleryOpen)
        {
            return;
        }

        RemoveDragOverlay();

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            // Don't show drop message any longer
            CloseToolTipMessage();

            ConfigureWindows.GetMainWindow.Activate();
            if (UC.GetStartUpUC is not null)
            {
                UC.GetStartUpUC.Visibility = Visibility.Collapsed;
            }
        });

        // Get files as strings
        if (e.Data.GetData(DataFormats.FileDrop, true) is not string[] files)
        {
            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var memoryStream = (MemoryStream)e.Data.GetData("text/x-moz-url");

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (memoryStream is not null)
                {
                    await LoadUrlAsync(memoryStream).ConfigureAwait(false);
                }
                else
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    await LoadPic.LoadPicFromStringAsync((string)e.Data.GetData(DataFormats.StringFormat))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                //
            }

            return;
        }

        if (files[0].IsSupported() == false)
        {
            if (files[0].IsArchive() == false)
            {
                if (Directory.Exists(files[0]) == false)
                {
                    return;
                }
            }
        }

        // Check if same file
        if (files.Length == 1 && Pics.Count > 0 && FolderIndex < Pics.Count)
        {
            if (files[0] == Pics[FolderIndex])
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
        }

        BackupPath = files[0];
        var fileInfo = new FileInfo(files[0]);

        //detect whether it's a directory or file
        if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
        {
            await LoadPic.LoadPicFromFolderAsync(fileInfo).ConfigureAwait(false);
        }
        else
        {
            await LoadPic.LoadPiFromFileAsync(files[0], fileInfo).ConfigureAwait(false);
        }

        // Open additional windows if multiple files dropped
        // TODO figure something out if the setting to files in the same window is enabled
        foreach (var file in files.Skip(1))
        {
            Core.ProcessHandling.ProcessHelper.StartNewProcess(file);
        }
        RemoveDragOverlay();
    }

    private static async Task LoadUrlAsync(MemoryStream memoryStream)
    {
        var dataStr = Encoding.Unicode.GetString(memoryStream.ToArray());
        var parts = dataStr.Split((char)10);

        await HttpFunctions.LoadPicFromUrlAsync(parts[0]).ConfigureAwait(false);
        await memoryStream.DisposeAsync().ConfigureAwait(false);
    }

    private static void AddDragOverlay()
    {
        _dropOverlay = new DragDropOverlay
        {
            Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth,
            Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight
        };
        ConfigureWindows.GetMainWindow.TopLayer.Children.Add(_dropOverlay);
    }

    private static void UpdateDragOverlay(UIElement element)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (element is null)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(UpdateDragOverlay)} element null");
#endif
            return;
        }

        try
        {
            _dropOverlay?.UpdateContent(element);
        }
        catch (Exception)
        {
            return;
        }
    }

    public static void RemoveDragOverlay()
    {
        try
        {
            if (ConfigureWindows.GetMainWindow.Dispatcher.CheckAccess())
            {
                Set();
            }
            else
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(Set);
            }

            return;
            
            void Set()
            {
                ConfigureWindows.GetMainWindow.TopLayer.Children.Remove(_dropOverlay);
                
                _dropOverlay = null;
            }
        }
        catch (Exception e)
        {
            //
        }

        try
        {
            UIHelper.RemoveAllInstancesOfType<DragDropOverlay?>(ConfigureWindows.GetMainWindow.TopLayer);
            UIHelper.RemoveAllInstancesOfType<DragDropOverlayPic?>(ConfigureWindows.GetMainWindow.TopLayer);
        }
        catch (Exception e)
        {
            //
        }

    }
}