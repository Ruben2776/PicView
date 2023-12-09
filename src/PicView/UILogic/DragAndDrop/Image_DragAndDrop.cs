using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.ProcessHandling;
using PicView.Properties;
using PicView.Views.UserControls.Misc;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.ImageHandling.Thumbnails;
using static PicView.UILogic.Tooltip;

namespace PicView.UILogic.DragAndDrop
{
    internal static class ImageDragAndDrop
    {
        /// <summary>
        /// Backup of image
        /// </summary>
        private static DragDropOverlay? _dropOverlay;

        /// <summary>
        /// Show image or thumbnail preview on drag enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static async Task Image_DragEnter(object sender, DragEventArgs e)
        {
            if (GalleryFunctions.IsGalleryOpen)
                return;

            AddDragOverlay();

            UIElement? element = null;

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
                if (Settings.Default.IncludeSubDirectories || Directory.GetFiles(files[0]).Length > 0)
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

                var actualWidth = 0;
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    actualWidth = (int)ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
                });
                var thumb = await GetBitmapSourceThumbAsync(files[0], actualWidth).ConfigureAwait(false);
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    element = new DragDropOverlayPic(thumb);
                });
            }

            // Tell that it's succeeded
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;

            if (element == null) return;
            if (_dropOverlay != null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => { UpdateDragOverlay(element); });
            }
        }

        /// <summary>
        /// Logic for handling when the cursor leaves drag area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Image_DragLeave(object sender, DragEventArgs e)
        {
            // Switch to previous image if available

            if (_dropOverlay != null)
            {
                RemoveDragOverlay();
            }
            else if (ConfigureWindows.GetMainWindow.TitleText.Text ==
                     Application.Current.Resources["NoImage"] as string)
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

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, RemoveDragOverlay);

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                // Don't show drop message any longer
                CloseToolTipMessage();

                ConfigureWindows.GetMainWindow.Activate();
            });

            // Get files as strings
            if (e.Data.GetData(DataFormats.FileDrop, true) is not string[] files)
            {
                await Task.Run(async () =>
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
                });

                return;
            }

            // Check if same file
            if (files.Length == 1 && Pics.Count > 0)
            {
                if (files[0] == Pics[FolderIndex])
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }
            }

            await Task.Run(async () =>
            {
                if (files[0].IsSupported() == false)
                {
                    if (Directory.Exists(files[0]))
                    {
                        await LoadPic.LoadPicFromFolderAsync(files[0]).ConfigureAwait(false);
                    }
                    else if (files[0].IsArchive())
                    {
                        await LoadPic.LoadPicFromArchiveAsync(files[0]).ConfigureAwait(false);
                    }
                }
                else
                {
                    await LoadPic.LoadPicFromStringAsync(files[0]).ConfigureAwait(false);
                }

                // Open additional windows if multiple files dropped
                foreach (var file in files.Skip(1))
                {
                    ProcessLogic.StartProcessWithFileArgument(file);
                }
            });
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
                _dropOverlay.UpdateContent(element);
            }
            catch (Exception)
            {
                return;
            }
        }

        private static void RemoveDragOverlay()
        {
            ConfigureWindows.GetMainWindow.TopLayer.Children.Remove(_dropOverlay);
            _dropOverlay = null;
        }
    }
}