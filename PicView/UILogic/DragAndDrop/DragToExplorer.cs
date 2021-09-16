using PicView.FileHandling;
using PicView.UILogic.TransformImage;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic.DragAndDrop
{
    internal static class DragToExplorer
    {
        internal static async void DragFile(object sender, MouseButtonEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null
                || Keyboard.Modifiers != ModifierKeys.Control
                || Keyboard.Modifiers == ModifierKeys.Shift
                || Keyboard.Modifiers == ModifierKeys.Alt
                || Properties.Settings.Default.FullscreenGallery
                || Properties.Settings.Default.Fullscreen
                || Scroll.IsAutoScrolling)
            {
                return;
            }

            if (ConfigureWindows.GetMainWindow.TitleText.IsFocused)
            {
                EditTitleBar.Refocus();
                return;
            }

            string file;

            if (Pics.Count == 0)
            {
                try
                {
                    // Download from internet
                    // TODO check file exceptions and errors
                    string url = FileFunctions.GetURL(ConfigureWindows.GetMainWindow.TitleText.Text);
                    if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) // Check if from web
                    {
                        file = await WebFunctions.DownloadData(url, false).ConfigureAwait(false);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
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

            FrameworkElement senderElement = sender as FrameworkElement;
            DataObject dragObj = new DataObject();
            dragObj.SetFileDropList(new StringCollection() { file });
            DragDrop.DoDragDrop(senderElement, dragObj, DragDropEffects.Copy);

            e.Handled = true;
        }
    }
}