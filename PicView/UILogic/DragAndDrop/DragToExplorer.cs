using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic.TransformImage;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic.DragAndDrop
{
    internal static class DragToExplorer
    {
        internal static void DragFile(object sender, MouseButtonEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null
                || Keyboard.Modifiers != ModifierKeys.Control
                || Keyboard.Modifiers == ModifierKeys.Shift
                || Keyboard.Modifiers == ModifierKeys.Alt
                || GalleryFunctions.IsHorizontalOpen
                || Settings.Default.Fullscreen
                || Scroll.IsAutoScrolling
                || ZoomLogic.IsZoomed)
            {
                return;
            }

            if (ConfigureWindows.GetMainWindow.TitleText.IsFocused)
            {
                EditTitleBar.Refocus();
                return;
            }

            string? file;
            if (Pics.Count == 0)
            {
                try
                {
                    // Check if from URL and locate it
                    string url = FileFunctions.RetrieveFromURL();
                    if (!string.IsNullOrEmpty(url))
                    {
                        file = ArchiveExtraction.TempFilePath;
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Tooltip.ShowTooltipMessage(ex);
                    return;
                }
            }
            else if (Pics.Count > FolderIndex)
            {
                file = Pics[FolderIndex];
            }
            else
            {
                return;
            }
            if (file == null) return;

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                FrameworkElement? senderElement = sender as FrameworkElement;
                if (senderElement == null) return;
                DataObject dragObj = new DataObject();
                dragObj.SetFileDropList(new StringCollection { file });
                DragDrop.DoDragDrop(senderElement, dragObj, DragDropEffects.Copy);
            });

            e.Handled = true;
        }
    }
}