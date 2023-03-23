using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.ProcessHandling;
using PicView.Properties;
using PicView.Views.UserControls.Misc;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.ImageHandling.Thumbnails;
using static PicView.UILogic.Tooltip;

namespace PicView.UILogic.DragAndDrop
{
    internal static class Image_DragAndDrop
    {
        /// <summary>
        /// Backup of image
        /// </summary>
        private static DragDropOverlay? DropOverlay;

        /// <summary>
        /// Show image or thumbnail preview on drag enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Image_DragEnter(object sender, DragEventArgs e)
        {
            if (GalleryFunctions.IsHorizontalOpen) return;

            UIElement? element = null;

            if (e.Data.GetData(DataFormats.FileDrop, true) is not string[] files)
            {
                var data = e.Data.GetData(DataFormats.Text);

                if (data != null) // Check if from web)
                {
                    // Link
                    element = new LinkChain();
                }
                else
                {
                    return;
                }
            }
            else if (Directory.Exists(files[0]))
            {
                if (Settings.Default.IncludeSubDirectories || Directory.GetFiles(files[0]).Length > 0)
                {
                    // Folder
                    element = new FolderIcon();
                }
                else
                {
                    return;
                }
            }
            else if (SupportedFiles.IsArchive(files[0]))
            {
                // Archive
                element = new ZipIcon();
            }
            else if (SupportedFiles.IsSupported(files[0]))
            {
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
                // File
                var thumb = GetBitmapSourceThumb(new FileInfo(files[0]), 300);
                element = new DragDropOverlayPic(thumb.Thumb);
            }

            // Tell that it's succeeded
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;

            if (element != null)
            {
                if (DropOverlay == null)
                {
                    AddDragOverlay(element);
                }
            }
        }

        /// <summary>
        /// Logic for handling when the cursor leaves drag area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Image_DragLeave(object sender, DragEventArgs e)
        {
            // TODO fix base64 image not returning to normal

            // Switch to previous image if available

            if (DropOverlay != null)
            {
                RemoveDragOverlay();
            }
            else if (ConfigureWindows.GetMainWindow.TitleText.Text == Application.Current.Resources["NoImage"] as string)
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
            if (GalleryFunctions.IsHorizontalOpen)
            {
                return;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                RemoveDragOverlay());

            // Get files as strings
            if (e.Data.GetData(DataFormats.FileDrop, true) is not string[] files)
            {
                await LoadURLAsync(e).ConfigureAwait(false);
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

            if (SupportedFiles.IsSupported(files[0]) == false)
            {
                if (Directory.Exists(files[0]))
                {
                    await LoadPic.LoadPicFromFolderAsync(files[0]).ConfigureAwait(false);
                }
                else if (SupportedFiles.IsArchive(files[0]))
                {
                    await LoadPic.LoadPicFromArchiveAsync(files[0]).ConfigureAwait(false);
                }
            }
            else
            {
                await LoadPic.LoadPicFromStringAsync(files[0]).ConfigureAwait(false);
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                // Don't show drop message any longer
                CloseToolTipMessage();

                ConfigureWindows.GetMainWindow.Activate();
            });

            // Open additional windows if multiple files dropped
            foreach (string file in files.Skip(1))
            {
                ProcessLogic.StartProcessWithFileArgument(file);
            }
        }
        static async Task LoadURLAsync(DragEventArgs e)
        {
            var memoryStream = (MemoryStream)e.Data.GetData("text/x-moz-url");
            if (memoryStream != null)
            {
                string dataStr = Encoding.Unicode.GetString(memoryStream.ToArray());
                string[] parts = dataStr.Split((char)10);

                await HttpFunctions.LoadPicFromURL(parts[0]).ConfigureAwait(false);
            }
        }

        private static void AddDragOverlay(UIElement element)
        {
            DropOverlay = new DragDropOverlay(element)
            {
                Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth,
                Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight
            };
            ConfigureWindows.GetMainWindow.topLayer.Children.Add(DropOverlay);
        }

        private static void RemoveDragOverlay()
        {
            ConfigureWindows.GetMainWindow.topLayer.Children.Remove(DropOverlay);
            DropOverlay = null;
        }
    }
}