using PicView.Library;
using PicView.UI.TransformImage;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;
using static PicView.SystemIntegration.NativeMethods;

namespace PicView.UI.DragAndDrop
{
    internal static class DragToExplorer
    {
        internal static Window dragdropWindow;

        internal static void DragFile(object sender, MouseButtonEventArgs e)
        {
            if (ZoomLogic.ZoomValue > 1.0
                || TheMainWindow.MainImage.Source == null
                || Keyboard.Modifiers == ModifierKeys.Control
                || Keyboard.Modifiers == ModifierKeys.Shift
                || Properties.Settings.Default.PicGallery == 2
                || Scroll.IsAutoScrolling)
            {
                return;
            }

            var pos = Utilities.GetMousePos(TheMainWindow.ParentContainer);

            //Tooltip.ShowTooltipMessage($"x = {pos.X} y = {pos.Y}");
            // Only target center of image
            if (pos.Y < 380 || pos.Y > 1040)
            {
                return;
            }

            if (TheMainWindow.TitleText.IsFocused)
            {
                EditTitleBar.Refocus();
                return;
            }

            string file;

            if (Pics.Count == 0)
            {
                string url = Utilities.GetURL(TheMainWindow.TitleText.Text);
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) // Check if from web
                {
                    // Create temp directory
                    var tempPath = Path.GetTempPath();
                    var fileName = Path.GetFileName(url);

                    // Download to it
                    using var webClient = new System.Net.WebClient();
                    Directory.CreateDirectory(tempPath);
                    webClient.DownloadFileAsync(new Uri(url), tempPath + fileName);

                    file = tempPath + fileName;
                }
                else
                {
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

            if (dragdropWindow == null)
            {
                CreateDragDropWindow(TheMainWindow.MainImage);
            }
            else if (!dragdropWindow.IsVisible)
            {
                dragdropWindow.Show();
                UpdateDragDropWindow(TheMainWindow.MainImage);
            }

            FrameworkElement senderElement = sender as FrameworkElement;
            DataObject dragObj = new DataObject();
            dragObj.SetFileDropList(new StringCollection() { file });
            DragDrop.AddQueryContinueDragHandler(senderElement, DragContrinueHandler);
            DragDrop.DoDragDrop(senderElement, dragObj, DragDropEffects.Copy);

            e.Handled = true;
        }

        private static void DragContrinueHandler(object sender, QueryContinueDragEventArgs e)
        {
            if (e.Action == DragAction.Continue && e.KeyStates != DragDropKeyStates.LeftMouseButton)
            {
                if (dragdropWindow == null)
                {
                    return;
                }
                dragdropWindow.Hide();
            }
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
                Opacity = .75
            };

            UpdateDragDropWindow(dragElement);

            dragdropWindow.Show();
        }

        private static void UpdateDragDropWindow(Visual dragElement)
        {
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


        internal static void UIElement_OnPreviewGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (dragdropWindow == null)
            {
                return;
            }

            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            dragdropWindow.Left = w32Mouse.X - 10;
            dragdropWindow.Top = w32Mouse.Y - 50;
        }
    }
}
