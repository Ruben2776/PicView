using PicView.ChangeTitlebar;
using PicView.Editing;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic.TransformImage;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.ChangeImage.Navigation;
using static PicView.SystemIntegration.NativeMethods;

namespace PicView.UILogic.DragAndDrop
{
    internal static class DragToExplorer
    {
        internal static Window? dragdropWindow;

        internal static void DragFile(object sender, MouseButtonEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null
                || Keyboard.Modifiers == ModifierKeys.Shift
                || Keyboard.Modifiers == ModifierKeys.Alt
                || GalleryFunctions.IsHorizontalFullscreenOpen
                || GalleryFunctions.IsHorizontalOpen
                || Settings.Default.Fullscreen
                || Scroll.IsAutoScrolling
                || ZoomLogic.IsZoomed
                || UC.GetQuickResize is not null && UC.GetQuickResize.Opacity > 0
                || UC.UserControls_Open()
                || ConfigureWindows.MainContextMenu.IsVisible
                || Color_Picking.IsRunning)
            {
                return;
            }

            if (ConfigureWindows.GetMainWindow.TitleText.IsFocused)
            {
                EditTitleBar.Refocus();
                return;
            }

            if (UC.GetCropppingTool is { IsVisible: true }) return;

            if (Properties.Settings.Default.ShowInterface == false)
            {
                if (Keyboard.Modifiers != ModifierKeys.Control)
                {
                    return;
                }
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
                file = Pics[FolderIndex];
            else return;
            if (file == null) return;

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                if (dragdropWindow == null)
                {
                    CreateDragDropWindow(ConfigureWindows.GetMainWindow.MainImage);
                }
                else if (!dragdropWindow.IsVisible)
                {
                    dragdropWindow.Show();
                    UpdateDragDropWindow(ConfigureWindows.GetMainWindow.MainImage);
                }

                FrameworkElement? senderElement = sender as FrameworkElement;
                DataObject dragObj = new DataObject();
                dragObj.SetFileDropList(new StringCollection() { file });
                if (senderElement is null) return;
                DragDrop.AddQueryContinueDragHandler(senderElement, DragContrinueHandler);
                DragDrop.DoDragDrop(senderElement, dragObj, DragDropEffects.Copy);
            });
        }

        private static void DragContrinueHandler(object sender, QueryContinueDragEventArgs e)
        {
            if (Color_Picking.IsRunning || e.Action == DragAction.Continue && e.KeyStates != DragDropKeyStates.LeftMouseButton)
            {
                dragdropWindow?.Hide();
                return;
            }
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            dragdropWindow.Left = w32Mouse.X + 10;
            dragdropWindow.Top = w32Mouse.Y - 50;
        }

        private static void CreateDragDropWindow(Visual dragElement)
        {
            dragdropWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                AllowDrop = false,
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true,
                ShowInTaskbar = false,
                Opacity = .75,
            };

            UpdateDragDropWindow(dragElement);

            dragdropWindow.Show();
        }

        private static void UpdateDragDropWindow(Visual dragElement)
        {
            var xWidth = Sizing.ScaleImage.XWidth;
            var xHeight = Sizing.ScaleImage.XHeight;

            var maxWidth = Math.Min(xWidth, xWidth / 1.8);
            var maxHeight = Math.Min(xHeight, xHeight / 1.8);
            var ratio = Math.Min(maxWidth / xHeight / 1.8, maxHeight / xWidth / 1.8);

            var r = new System.Windows.Shapes.Rectangle
            {
                Width = maxWidth * ratio,
                Height = maxHeight * ratio,
                Fill = new VisualBrush(dragElement)
            };
            dragdropWindow.Content = r;
        }
    }
}