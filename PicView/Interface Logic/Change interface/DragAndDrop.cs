using PicView.PreLoading;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static PicView.Fields;
using static PicView.Navigation;
using static PicView.Thumbnails;
using static PicView.Tooltip;

namespace PicView
{
    internal static class DragAndDrop
    {
        internal const string DragOverString = "Drop to load image";

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

            // Tell that it's succeeded
            e.Effects = DragDropEffects.Copy;
            isDraggedOver = e.Handled = true;
            ToolTipStyle(DragOverString, true);

            if (check.Value)
            {
                // If no image, fix it to container
                if (mainWindow.img.Source == null)
                {
                    mainWindow.img.Width = mainWindow.Scroller.ActualWidth;
                    mainWindow.img.Height = mainWindow.Scroller.ActualHeight;
                }
                else
                {
                    // Save our image so we can swap back to it later if neccesary
                    prevPicResource = mainWindow.img.Source;
                }

                // Load from preloader or thumbnails
                mainWindow.img.Source = Preloader.Contains(files[0]) ? Preloader.Load(files[0]) : GetBitmapSourceThumb(files[0]);
            }
        }

        /// <summary>
        /// Logic for handling when the cursor leaves drag area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Image_DragLeave(object sender, DragEventArgs e)
        {
            // Error handling
            if (!isDraggedOver)
            {
                return;
            }

            // Switch to previous image if available
            if (!canNavigate)
            {
                mainWindow.img.Source = null;
            }
            else if (prevPicResource != null)
            {
                mainWindow.img.Source = prevPicResource;
            }

            // Update status
            isDraggedOver = false;
        }

        /// <summary>
        /// Logic for handling the drop event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Image_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.Html) != null)
            {
                MemoryStream data = (MemoryStream)e.Data.GetData("text/x-moz-url");
                if (data != null)
                {
                    string dataStr = Encoding.Unicode.GetString(data.ToArray());
                    string[] parts = dataStr.Split((char)10);

                    Pic(parts[0]);
                    return;
                }
            }

            // Get files as strings
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

            // check if valid
            if (!Drag_Drop_Check(files).HasValue)
            {
                if (Path.GetExtension(files[0]) == ".url")
                {
                    Pic(files[0]);
                }
                else return;
            }

            // Load it
            Pic(files[0]);

            // Don't show drop message any longer
            CloseToolTipStyle();

            // Start multiple clients if user drags multiple files
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
