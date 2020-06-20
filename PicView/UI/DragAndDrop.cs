using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UI.Loading;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;
using static PicView.ImageHandling.Thumbnails;
using static PicView.Library.Fields;
using static PicView.UI.Tooltip;

namespace PicView.UI
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
            e.Handled = true;
            ShowTooltipMessage(DragOverString, true);

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
            // TODO fix base64 image not returning to normal

            // Switch to previous image if available
            if (!CanNavigate)
            {
                mainWindow.img.Source = null;
            }
            else if (prevPicResource != null)
            {
                mainWindow.img.Source = prevPicResource;
            }
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
                else if (Directory.Exists(files[0]))
                {
                    if (Directory.GetFiles(files[0]).Length > 0)
                    {
                        PicFolder(files[0]);
                    }
                    return;
                }
                else return;
            }

            // Load it
            Pic(files[0]);

            // Don't show drop message any longer
            CloseToolTipMessage();
            AjaxLoader.AjaxLoadingEnd();

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

        internal static void DragFile(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control || mainWindow.img.Source == null)
            {
                return;
            }

            FrameworkElement senderElement = sender as FrameworkElement;
            DataObject dragObj = new DataObject();
            dragObj.SetFileDropList(new StringCollection() { Pics[FolderIndex] });
            DragDrop.DoDragDrop(senderElement, dragObj, DragDropEffects.Copy);
        }
    }
}