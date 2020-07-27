using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UILogic.Loading;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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
        private static ImageSource prevPicResource;

        /// <summary>
        /// Check if dragged file is valid,
        /// returns false for valid file with no thumbnail,
        /// true for valid file with thumbnail
        /// and null for invalid file
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private static bool? Drag_Drop_Check(string[] files)
        {
            // Return if file strings are null
            if (files == null)
            {
                return null;
            }

            if (files[0] == null)
            {
                return null;
            }

            // Return status of useable file
            return SupportedFiles.IsSupportedFileWithArchives(Path.GetExtension(files[0]));
        }

        /// <summary>
        /// Show image or thumbnail preview on drag enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Image_DragEnter(object sender, DragEventArgs e)
        {
            // Error handling
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            var check = Drag_Drop_Check(files);

            // Do nothing for invalid files
            if (!check.HasValue)
            {
                return;
            }

            if (!check.Value)
            {
                return;
            }

            // Check if same file
            if (files.Length == 1 && Pics.Count > 0)
            {
                if (files[0] == Pics[FolderIndex])
                {
                    return;
                }
            }

            // Tell that it's succeeded
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            ShowTooltipMessage(Application.Current.Resources["DragOverString"] as string, true);

            // If no image, fix it to container
            if (LoadWindows.GetMainWindow.MainImage.Source == null)
            {
                LoadWindows.GetMainWindow.MainImage.Width = LoadWindows.GetMainWindow.Scroller.ActualWidth;
                LoadWindows.GetMainWindow.MainImage.Height = LoadWindows.GetMainWindow.Scroller.ActualHeight;
            }
            else
            {
                // Save our image so we can swap back to it later if neccesary
                prevPicResource = LoadWindows.GetMainWindow.MainImage.Source;
            }

            // Load from preloader or thumbnails
            var thumb = Preloader.Contains(files[0]) ? Preloader.Load(files[0]) : GetBitmapSourceThumb(files[0]);

            if (thumb != null)
            {
                LoadWindows.GetMainWindow.MainImage.Source = thumb;
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

            if (prevPicResource != null)
            {
                LoadWindows.GetMainWindow.MainImage.Source = prevPicResource;
                prevPicResource = null;
            }
            else if (LoadWindows.GetMainWindow.TitleText.Text == Application.Current.Resources["NoImage"] as string)
            {
                LoadWindows.GetMainWindow.MainImage.Source = null;
            }

            CloseToolTipMessage();
        }

        /// <summary>
        /// Logic for handling the drop event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Image_Drop(object sender, DragEventArgs e)
        {
            // Load dropped URL
            if (e.Data.GetData(DataFormats.Html) != null)
            {
                MemoryStream data = (MemoryStream)e.Data.GetData("text/x-moz-url");
                if (data != null)
                {
                    string dataStr = Encoding.Unicode.GetString(data.ToArray());
                    string[] parts = dataStr.Split((char)10);

                    Pic(parts[0]);
                    LoadWindows.GetMainWindow.Activate();
                    return;
                }
            }

            // Get files as strings
            if (!(e.Data.GetData(DataFormats.FileDrop, true) is string[] files))
            {
                return;
            }

            // check if valid
            if (!Drag_Drop_Check(files).HasValue)
            {
                if (Path.GetExtension(files[0]) == ".url")
                {
                    Pic(files[0]);
                }
                else if (Directory.Exists(files[0]))
                {
                    if (Directory.GetFiles(files[0]).Length > 0)
                    {
                        PicFolder(files[0]);
                        LoadWindows.GetMainWindow.Activate();
                    }
                    return;
                }
                else
                {
                    return;
                }
            }

            // Check if same file
            if (files.Length == 1 && Pics.Count > 0)
            {
                if (files[0] == Pics[FolderIndex])
                {
                    return;
                }
            }

            // Load it
            Pic(files[0]);

            // Don't show drop message any longer
            CloseToolTipMessage();

            LoadWindows.GetMainWindow.Activate();

            // Start multiple clients if user drags multiple files
            // TODO no longer working after converting to .NET Core...
            if (files.Length > 0)
            {
                Parallel.For(1, files.Length, x =>
                {
                    var myProcess = new Process
                    {
                        StartInfo = {
                                    FileName = Assembly.GetExecutingAssembly().Location,
                                    Arguments = files[x]
                                }
                    };
                    myProcess.Start();
                });
            }

            // Save memory, make prevPicResource null
            if (prevPicResource != null)
            {
                prevPicResource = null;
            }
        }
    }
}